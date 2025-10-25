using LifeHub.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.EntityFrameworkCore;
using LifeHub.Data;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace LifeHub.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly string _connectionString;
        private readonly ILogger<SubscriptionService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;

        public SubscriptionService(IConfiguration configuration,
                                 ILogger<SubscriptionService> logger,
                                 ApplicationDbContext context,
                                 IDistributedCache cache)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _logger = logger;
            _context = context;
            _cache = cache;
        }

        public async Task<List<SubscriptionPlan>> GetActivePlansAsync()
        {
            var cacheKey = "subscription:plans:active";
            List<SubscriptionPlan> plans;

            // Intentar obtener de Redis
            var cachedPlans = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedPlans))
            {
                _logger.LogInformation("✅ Cache HIT - Planes activos cargados desde Redis");
                return JsonSerializer.Deserialize<List<SubscriptionPlan>>(cachedPlans);
            }

            _logger.LogInformation("🔄 Cache MISS - Cargando planes activos desde BD");

            // Tu código existente para cargar desde BD
            plans = new List<SubscriptionPlan>();
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    const string sql = @"
                SELECT id, name, short_description, long_description, price, duration_days, 
                       is_active, max_habits, max_transactions, has_community_access, 
                       has_advanced_analytics, has_ai_features, storage_mb, color_code, 
                       is_featured, sort_order, features, created_at
                FROM subscription_plans 
                WHERE is_active = true 
                ORDER BY sort_order ASC, price ASC";

                    using (var command = new NpgsqlCommand(sql, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var featuresJson = reader.IsDBNull(16) ? "{}" : reader.GetString(16);

                            var plan = new SubscriptionPlan
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                ShortDescription = reader.GetString(2),
                                LongDescription = reader.GetString(3),
                                Price = reader.GetDecimal(4),
                                DurationDays = reader.GetInt32(5),
                                IsActive = reader.GetBoolean(6),
                                MaxHabits = reader.GetInt32(7),
                                MaxTransactions = reader.GetInt32(8),
                                HasCommunityAccess = reader.GetBoolean(9),
                                HasAdvancedAnalytics = reader.GetBoolean(10),
                                HasAIFeatures = reader.GetBoolean(11),
                                StorageMB = reader.GetInt32(12),
                                ColorCode = reader.GetString(13),
                                IsFeatured = reader.GetBoolean(14),
                                SortOrder = reader.GetInt32(15),
                                Features = JsonDocument.Parse(featuresJson),
                                CreatedAt = reader.GetDateTime(17)
                            };

                            plans.Add(plan);
                        }
                    }
                }

                // Guardar en Redis por 30 minutos
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                };
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(plans), cacheOptions);

                _logger.LogInformation("💾 Planes activos guardados en Redis por 30 minutos");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener planes de suscripción");
                throw;
            }

            return plans;
        }

        public async Task<SubscriptionPlan?> GetPlanByIdAsync(int id)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    const string sql = @"
                        SELECT id, name, short_description, long_description, price, duration_days, 
                               is_active, max_habits, max_transactions, has_community_access, 
                               has_advanced_analytics, has_ai_features, storage_mb, color_code, 
                               is_featured, sort_order, features, created_at
                        FROM subscription_plans 
                        WHERE id = @id AND is_active = true";

                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("id", id);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var featuresJson = reader.IsDBNull(16) ? "{}" : reader.GetString(16);

                                return new SubscriptionPlan
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    ShortDescription = reader.GetString(2),
                                    LongDescription = reader.GetString(3),
                                    Price = reader.GetDecimal(4),
                                    DurationDays = reader.GetInt32(5),
                                    IsActive = reader.GetBoolean(6),
                                    MaxHabits = reader.GetInt32(7),
                                    MaxTransactions = reader.GetInt32(8),
                                    HasCommunityAccess = reader.GetBoolean(9),
                                    HasAdvancedAnalytics = reader.GetBoolean(10),
                                    HasAIFeatures = reader.GetBoolean(11),
                                    StorageMB = reader.GetInt32(12),
                                    ColorCode = reader.GetString(13),
                                    IsFeatured = reader.GetBoolean(14),
                                    SortOrder = reader.GetInt32(15),
                                    Features = JsonDocument.Parse(featuresJson),
                                    CreatedAt = reader.GetDateTime(17)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener plan por ID: {Id}", id);
                throw;
            }

            return null;
        }

        public async Task AssignPlanToUserAsync(string userId, int planId)
        {
            try
            {
                _logger.LogInformation("🔄 INICIANDO AssignPlanToUserAsync - User: {UserId}, Plan: {PlanId}", userId, planId);

                var plan = await GetPlanByIdAsync(planId);
                if (plan == null)
                {
                    _logger.LogError("❌ Plan {PlanId} no encontrado", planId);
                    throw new ArgumentException("Plan no válido");
                }

                var endDate = DateTime.UtcNow.AddDays(plan.DurationDays);
                _logger.LogInformation("📅 Nueva fecha de fin: {EndDate}", endDate);

                // Buscar TODAS las suscripciones del usuario
                var existingSubscriptions = await _context.UserSubscriptions
                    .Where(us => us.UserId == userId)
                    .ToListAsync();

                _logger.LogInformation("📊 Suscripciones existentes encontradas: {Count}", existingSubscriptions.Count);

                // Desactivar TODAS las suscripciones existentes
                foreach (var sub in existingSubscriptions)
                {
                    sub.IsActive = false;
                    _logger.LogInformation("❌ Desactivada suscripción: {SubscriptionId}", sub.Id);
                }

                // Crear NUEVA suscripción (siempre crear nueva en lugar de actualizar)
                var newSubscription = new UserSubscription
                {
                    UserId = userId,
                    PlanId = planId,
                    StartDate = DateTime.UtcNow,
                    EndDate = endDate,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserSubscriptions.Add(newSubscription);
                _logger.LogInformation("✅ Nueva suscripción creada - Plan: {PlanId}, Activa: true", planId);

                // Guardar cambios
                var result = await _context.SaveChangesAsync();
                _logger.LogInformation("💾 Cambios guardados en BD - Filas afectadas: {RowsAffected}", result);

                // Limpiar cache
                await _cache.RemoveAsync($"subscription:user:{userId}");
                _logger.LogInformation("🗑️ Cache limpiado para usuario: {UserId}", userId);

                _logger.LogInformation("🎉 Plan {PlanId} asignado EXITOSAMENTE al usuario {UserId}", planId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 ERROR CRÍTICO al asignar plan {PlanId} al usuario {UserId}", planId, userId);
                throw;
            }
        }

        public async Task<UserSubscription?> GetUserSubscriptionAsync(string userId)
{
    try
    {
        _logger.LogInformation("🔍 Buscando suscripción para usuario: {UserId}", userId);

        using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            // ✅ OBTENER LA SUSCRIPCIÓN ACTIVA MÁS RECIENTE
            const string sql = @"
                SELECT us.id, us.user_id, us.plan_id, us.start_date, us.end_date, 
                       us.is_active, us.created_at,
                       p.name, p.price, p.duration_days, p.max_transactions, p.max_habits,
                       p.""MaxBudgets"", p.has_advanced_analytics, p.has_ai_features, p.color_code
                FROM user_subscriptions us
                INNER JOIN subscription_plans p ON us.plan_id = p.id
                WHERE us.user_id = @userId AND us.is_active = true
                ORDER BY us.created_at DESC, us.id DESC
                LIMIT 1";

            using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("userId", userId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var subscription = new UserSubscription
                        {
                            Id = reader.GetInt32(0),
                            UserId = reader.GetString(1),
                            PlanId = reader.GetInt32(2),
                            StartDate = reader.GetDateTime(3),
                            EndDate = reader.GetDateTime(4),
                            IsActive = reader.GetBoolean(5),
                            CreatedAt = reader.GetDateTime(6),
                            Plan = new SubscriptionPlan
                            {
                                Id = reader.GetInt32(2),
                                Name = reader.GetString(7),
                                Price = reader.GetDecimal(8),
                                DurationDays = reader.GetInt32(9),
                                MaxTransactions = reader.GetInt32(10),
                                MaxHabits = reader.GetInt32(11),
                                MaxBudgets = reader.GetInt32(12),
                                HasAdvancedAnalytics = reader.GetBoolean(13),
                                HasAIFeatures = reader.GetBoolean(14),
                                ColorCode = reader.GetString(15)
                            }
                        };

                        _logger.LogInformation("✅ Suscripción encontrada: {PlanName} (ID: {PlanId})", 
                            subscription.Plan.Name, subscription.PlanId);
                        return subscription;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ No se encontró suscripción activa para usuario {UserId}", userId);
                        
                        // ✅ FALLBACK: Buscar cualquier suscripción (aunque no esté activa)
                        return await GetAnyUserSubscriptionAsync(userId);
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Error al obtener suscripción del usuario {UserId}", userId);
        return null;
    }
}

        // ✅ MÉTODO DE FALLBACK: Obtener cualquier suscripción del usuario
        private async Task<UserSubscription?> GetAnyUserSubscriptionAsync(string userId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    const string sql = @"
                SELECT us.id, us.user_id, us.plan_id, us.start_date, us.end_date, 
                       us.is_active, us.created_at,
                       p.name, p.price, p.duration_days, p.max_transactions, p.max_habits,
                       p.""MaxBudgets"", p.has_advanced_analytics, p.has_ai_features, p.color_code
                FROM user_subscriptions us
                INNER JOIN subscription_plans p ON us.plan_id = p.id
                WHERE us.user_id = @userId
                ORDER BY us.created_at DESC, us.id DESC
                LIMIT 1";

                    using (var command = new NpgsqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("userId", userId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var subscription = new UserSubscription
                                {
                                    Id = reader.GetInt32(0),
                                    UserId = reader.GetString(1),
                                    PlanId = reader.GetInt32(2),
                                    StartDate = reader.GetDateTime(3),
                                    EndDate = reader.GetDateTime(4),
                                    IsActive = reader.GetBoolean(5),
                                    CreatedAt = reader.GetDateTime(6),
                                    Plan = new SubscriptionPlan
                                    {
                                        Id = reader.GetInt32(2),
                                        Name = reader.GetString(7),
                                        Price = reader.GetDecimal(8),
                                        DurationDays = reader.GetInt32(9),
                                        MaxTransactions = reader.GetInt32(10),
                                        MaxHabits = reader.GetInt32(11),
                                        MaxBudgets = reader.GetInt32(12),
                                        HasAdvancedAnalytics = reader.GetBoolean(13),
                                        HasAIFeatures = reader.GetBoolean(14),
                                        ColorCode = reader.GetString(15)
                                    }
                                };

                                _logger.LogInformation("✅ Suscripción de fallback encontrada: {PlanName} (Activa: {IsActive})",
                                    subscription.Plan.Name, subscription.IsActive);
                                return subscription;
                            }
                        }
                    }
                }

                _logger.LogWarning("🔍 No se encontró NINGUNA suscripción para usuario {UserId}", userId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en fallback al obtener suscripción del usuario {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> UserHasFeatureAccessAsync(string userId, string feature)
        {
            var subscription = await GetUserSubscriptionAsync(userId);
            if (subscription?.Plan == null) return false;

            return feature switch
            {
                "community" => subscription.Plan.HasCommunityAccess,
                "analytics" => subscription.Plan.HasAdvancedAnalytics,
                "ai" => subscription.Plan.HasAIFeatures,
                _ => false
            };
        }

        public async Task<bool> CanUserCreateHabitAsync(string userId)
        {
            var subscription = await GetUserSubscriptionAsync(userId);
            if (subscription?.Plan == null) return false;

            var currentHabitsCount = await _context.Habits
                .CountAsync(h => h.UserId == userId);

            return currentHabitsCount < subscription.Plan.MaxHabits;
        }

        public async Task<bool> CanUserCreateTransactionAsync(string userId)
        {
            var subscription = await GetUserSubscriptionAsync(userId);
            if (subscription?.Plan == null) return false;

            // FIX: Usar DateTime.UtcNow explícitamente y crear el primer día del mes en UTC
            var now = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            try
            {
                var currentTransactionsCount = await _context.FinancialTransactions
                    .CountAsync(t => t.UserId == userId && t.TransactionDate >= firstDayOfMonth);

                return currentTransactionsCount < subscription.Plan.MaxTransactions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar límite de transacciones para usuario {UserId}", userId);
                // En caso de error, permitir la creación como fallback
                return true;
            }
        }

        // ✅ NUEVO MÉTODO: Verificar límite de presupuestos
        public async Task<bool> CanUserCreateBudgetAsync(string userId)
        {
            var subscription = await GetUserSubscriptionAsync(userId);
            if (subscription?.Plan == null) return false;

            try
            {
                var currentBudgetsCount = await _context.Budgets
                    .CountAsync(b => b.UserId == userId);

                // ✅ AHORA usa MaxBudgets REAL del plan
                return currentBudgetsCount < subscription.Plan.MaxBudgets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar límite de presupuestos para usuario {UserId}", userId);
                return true;
            }
        }

        // ✅ NUEVO MÉTODO: Obtener uso actual de transacciones
        public async Task<(int current, int max)> GetTransactionUsageAsync(string userId)
        {
            var subscription = await GetUserSubscriptionAsync(userId);
            if (subscription?.Plan == null) return (0, 50); // Fallback a gratis

            var now = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            try
            {
                var currentCount = await _context.FinancialTransactions
                    .CountAsync(t => t.UserId == userId && t.TransactionDate >= firstDayOfMonth);

                return (currentCount, subscription.Plan.MaxTransactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener uso de transacciones para usuario {UserId}", userId);
                return (0, subscription.Plan.MaxTransactions);
            }
        }

        // ✅ NUEVO MÉTODO: Obtener uso actual de presupuestos
        public async Task<(int current, int max)> GetBudgetUsageAsync(string userId)
        {
            var subscription = await GetUserSubscriptionAsync(userId);
            if (subscription?.Plan == null) return (0, 5); // Fallback a gratis

            try
            {
                var currentCount = await _context.Budgets.CountAsync(b => b.UserId == userId);

                // ✅ AHORA usa MaxBudgets REAL del plan
                return (currentCount, subscription.Plan.MaxBudgets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener uso de presupuestos para usuario {UserId}", userId);
                return (0, subscription?.Plan?.MaxBudgets ?? 5);
            }
        }
    }
}