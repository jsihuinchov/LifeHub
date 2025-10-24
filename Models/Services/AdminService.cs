using LifeHub.Data;
using LifeHub.Models;
using LifeHub.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LifeHub.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly string _connectionString;
        private readonly ILogger<AdminService> _logger;

        public AdminService(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ILogger<AdminService> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _logger = logger;
        }

        public async Task<AdminDashboardViewModel> GetDashboardMetricsAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var metrics = new AdminDashboardViewModel();

                // Total de usuarios - CORREGIDO: usar COUNT(*) sin filtro de fecha
                var totalUsersCmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"AspNetUsers\"", connection);
                metrics.TotalUsers = Convert.ToInt32(await totalUsersCmd.ExecuteScalarAsync());

                // Usuarios activos (no baneados) - CORREGIDO
                var activeUsersCmd = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM \"AspNetUsers\" WHERE \"LockoutEnd\" IS NULL OR \"LockoutEnd\" < NOW()",
                    connection);
                metrics.ActiveUsers = Convert.ToInt32(await activeUsersCmd.ExecuteScalarAsync());

                // Usuarios baneados - CORREGIDO
                var bannedUsersCmd = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM \"AspNetUsers\" WHERE \"LockoutEnd\" IS NOT NULL AND \"LockoutEnd\" > NOW()",
                    connection);
                metrics.BannedUsers = Convert.ToInt32(await bannedUsersCmd.ExecuteScalarAsync());

                // Nuevos usuarios hoy - CORREGIDO: eliminar referencia a CreatedAt
                // Como no tenemos CreatedAt, contamos todos los usuarios
                metrics.NewUsersToday = 0; // Temporalmente 0

                // Total de hábitos - CORREGIDO
                var habitsCmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"Habits\"", connection);
                metrics.TotalHabits = Convert.ToInt32(await habitsCmd.ExecuteScalarAsync());

                // Hábitos activos - CORREGIDO
                var activeHabitsCmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"Habits\" WHERE \"IsActive\" = true", connection);
                metrics.ActiveHabits = Convert.ToInt32(await activeHabitsCmd.ExecuteScalarAsync());

                // Total de transacciones - CORREGIDO
                var transactionsCmd = new NpgsqlCommand("SELECT COUNT(*) FROM \"FinancialTransactions\"", connection);
                metrics.TotalTransactions = Convert.ToInt32(await transactionsCmd.ExecuteScalarAsync());

                // Ingresos mensuales - CORREGIDO: usar nombres de columna correctos
                var revenueCmd = new NpgsqlCommand(@"
            SELECT COALESCE(SUM(sp.price), 0) 
            FROM user_subscriptions us 
            JOIN subscription_plans sp ON us.plan_id = sp.id 
            WHERE us.is_active = true AND us.end_date > NOW()",
                    connection);
                metrics.MonthlyRevenue = Convert.ToDecimal(await revenueCmd.ExecuteScalarAsync());

                // Distribución de planes - CORREGIDO: usar nombres de columna correctos
                var planDistributionCmd = new NpgsqlCommand(@"
            SELECT sp.name, sp.color_code, COUNT(us.user_id) as user_count
            FROM subscription_plans sp
            LEFT JOIN user_subscriptions us ON sp.id = us.plan_id AND us.is_active = true
            WHERE sp.is_active = true
            GROUP BY sp.id, sp.name, sp.color_code
            ORDER BY user_count DESC",
                    connection);

                using var planReader = await planDistributionCmd.ExecuteReaderAsync();
                while (await planReader.ReadAsync())
                {
                    metrics.PlanDistribution.Add(new PlanDistributionItem
                    {
                        PlanName = planReader.GetString(0),
                        Color = planReader.GetString(1),
                        UserCount = planReader.GetInt32(2)
                    });
                }

                // Stats de crecimiento (simulados por ahora)
                metrics.GrowthStats = await GetGrowthStatsAsync(7);

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener métricas del dashboard admin");
                return new AdminDashboardViewModel();
            }
        }

        public async Task<List<AdminUserViewModel>> GetAllUsersAsync()
        {
            try
            {
                var users = await _context.Users
                    .OrderByDescending(u => u.Id == "admin-id-123") // Admin primero
                    .ThenByDescending(u => u.EmailConfirmed)
                    .Select(u => new AdminUserViewModel
                    {
                        Id = u.Id,
                        Email = u.Email!,
                        UserName = u.UserName!,
                        EmailConfirmed = u.EmailConfirmed,
                        PhoneNumber = u.PhoneNumber,
                        LockoutEnd = u.LockoutEnd,
                        LockoutEnabled = u.LockoutEnabled,
                        AccessFailedCount = u.AccessFailedCount
                    })
                    .ToListAsync();

                // Obtener roles y suscripciones para cada usuario usando consultas separadas
                foreach (var user in users)
                {
                    var identityUser = await _userManager.FindByIdAsync(user.Id);
                    if (identityUser != null)
                    {
                        user.Roles = await _userManager.GetRolesAsync(identityUser);
                    }

                    // Obtener suscripción usando consulta directa SQL para evitar problemas de mapeo
                    try
                    {
                        using var connection = new NpgsqlConnection(_connectionString);
                        await connection.OpenAsync();

                        var subscriptionCmd = new NpgsqlCommand(@"
                    SELECT us.user_id, us.plan_id, us.start_date, us.end_date, us.is_active,
                           sp.name, sp.price, sp.duration_days
                    FROM user_subscriptions us
                    JOIN subscription_plans sp ON us.plan_id = sp.id
                    WHERE us.user_id = @userId AND us.is_active = true
                    LIMIT 1", connection);

                        subscriptionCmd.Parameters.AddWithValue("userId", user.Id);

                        using var subscriptionReader = await subscriptionCmd.ExecuteReaderAsync();
                        if (await subscriptionReader.ReadAsync())
                        {
                            user.SubscriptionPlan = subscriptionReader.GetString(5); // sp.name
                            user.SubscriptionEndDate = subscriptionReader.GetDateTime(3); // us.end_date
                        }
                        else
                        {
                            user.SubscriptionPlan = "Sin plan";
                            user.SubscriptionEndDate = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error al obtener suscripción para usuario {UserId}", user.Id);
                        user.SubscriptionPlan = "Error al cargar";
                        user.SubscriptionEndDate = null;
                    }
                }

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener lista de usuarios");
                return new List<AdminUserViewModel>();
            }
        }

        public async Task<bool> BanUserAsync(string userId, string reason, bool permanent = true, int? days = null)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                DateTimeOffset lockoutEnd;
                if (permanent)
                {
                    lockoutEnd = DateTimeOffset.MaxValue;
                }
                else
                {
                    lockoutEnd = DateTimeOffset.UtcNow.AddDays(days ?? 30);
                }

                var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuario {UserId} baneado. Razón: {Reason}", userId, reason);
                }
                
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al banear usuario {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UnbanUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                var result = await _userManager.SetLockoutEndDateAsync(user, null);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuario {UserId} desbaneado", userId);
                }
                
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desbanear usuario {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ChangeUserPlanAsync(string userId, int planId)
        {
            try
            {
                var subscription = await _context.UserSubscriptions
                    .FirstOrDefaultAsync(us => us.UserId == userId && us.IsActive);

                if (subscription != null)
                {
                    subscription.PlanId = planId;
                    subscription.EndDate = DateTime.UtcNow.AddDays(30); // Renovar por 30 días
                }
                else
                {
                    var newSubscription = new UserSubscription
                    {
                        UserId = userId,
                        PlanId = planId,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddDays(30),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.UserSubscriptions.Add(newSubscription);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Plan cambiado a {PlanId} para usuario {UserId}", planId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar plan del usuario {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                // No permitir eliminar al admin principal
                if (userId == "admin-id-123") return false;

                var result = await _userManager.DeleteAsync(user);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ToggleUserRoleAsync(string userId, string role)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                var isInRole = await _userManager.IsInRoleAsync(user, role);
                
                if (isInRole)
                {
                    var result = await _userManager.RemoveFromRoleAsync(user, role);
                    return result.Succeeded;
                }
                else
                {
                    var result = await _userManager.AddToRoleAsync(user, role);
                    return result.Succeeded;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar rol del usuario {UserId}", userId);
                return false;
            }
        }

        public async Task<List<GrowthStatsViewModel>> GetGrowthStatsAsync(int days = 30)
        {
            var stats = new List<GrowthStatsViewModel>();
            
            for (int i = days - 1; i >= 0; i--)
            {
                var date = DateTime.Now.AddDays(-i).ToString("MM/dd");
                stats.Add(new GrowthStatsViewModel
                {
                    Date = date,
                    NewUsers = Random.Shared.Next(0, 10),
                    NewHabits = Random.Shared.Next(0, 20),
                    NewTransactions = Random.Shared.Next(0, 30)
                });
            }
            
            return await Task.FromResult(stats);
        }

        // Implementaciones restantes (simplificadas por ahora)
        public async Task<List<UserStatsViewModel>> GetUserStatsAsync()
        {
            return await Task.FromResult(new List<UserStatsViewModel>
            {
                new() { Period = "Hoy", NewUsers = 5, ActiveUsers = 120 },
                new() { Period = "Ayer", NewUsers = 3, ActiveUsers = 115 },
                new() { Period = "Esta semana", NewUsers = 25, ActiveUsers = 450 }
            });
        }

        public async Task<PlanDistributionViewModel> GetPlanDistributionAsync()
        {
            var distribution = new PlanDistributionViewModel();
            
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var cmd = new NpgsqlCommand(@"
                SELECT sp.name, sp.color_code, COUNT(us.user_id) as user_count
                FROM subscription_plans sp
                LEFT JOIN user_subscriptions us ON sp.id = us.plan_id AND us.is_active = true
                WHERE sp.is_active = true
                GROUP BY sp.id, sp.name, sp.color_code
                ORDER BY user_count DESC",
                connection);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                distribution.Distribution.Add(new PlanDistributionItem
                {
                    PlanName = reader.GetString(0),
                    Color = reader.GetString(1),
                    UserCount = reader.GetInt32(2)
                });
            }

            distribution.TotalUsers = distribution.Distribution.Sum(d => d.UserCount);
            
            // Calcular porcentajes
            foreach (var item in distribution.Distribution)
            {
                item.Percentage = distribution.TotalUsers > 0 ? 
                    (item.UserCount * 100m) / distribution.TotalUsers : 0;
            }

            return distribution;
        }

        public async Task<UsageStatsViewModel> GetUsageStatsAsync()
        {
            return await Task.FromResult(new UsageStatsViewModel
            {
                TotalHabitsCreated = 1500,
                TotalTransactionsCreated = 8500,
                TotalAppointmentsCreated = 300,
                TotalCommunitiesCreated = 45,
                ActiveHabitsToday = 120,
                TransactionsThisMonth = 650
            });
        }

        public async Task<List<SubscriptionPlan>> GetAllPlansAsync()
        {
            try
            {
                // Usar consulta directa SQL si el DbSet no funciona
                var plans = new List<SubscriptionPlan>();

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    const string sql = @"
                SELECT id, name, short_description, long_description, price, duration_days, 
                       is_active, max_habits, max_transactions, has_community_access, 
                       has_advanced_analytics, has_ai_features, storage_mb, color_code, 
                       is_featured, sort_order, created_at
                FROM subscription_plans 
                WHERE is_active = true 
                ORDER BY sort_order ASC, price ASC";

                    using (var command = new NpgsqlCommand(sql, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
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
                                CreatedAt = reader.GetDateTime(16)
                            };

                            plans.Add(plan);
                        }
                    }
                }

                return plans;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener planes");
                return new List<SubscriptionPlan>();
            }
        }

        public async Task<AdminUserViewModel?> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            var subscription = await _context.UserSubscriptions
                .Include(us => us.Plan)
                .FirstOrDefaultAsync(us => us.UserId == userId && us.IsActive);

            return new AdminUserViewModel
            {
                Id = user.Id,
                Email = user.Email!,
                UserName = user.UserName!,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                LockoutEnd = user.LockoutEnd,
                LockoutEnabled = user.LockoutEnabled,
                AccessFailedCount = user.AccessFailedCount,
                Roles = roles,
                SubscriptionPlan = subscription?.Plan?.Name ?? "Sin plan",
                SubscriptionEndDate = subscription?.EndDate
            };
        }
    }
}