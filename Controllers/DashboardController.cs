using LifeHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using LifeHub.Models;
using System.Linq;
using System.Collections.Generic;
using LifeHub.Models.Entities;
using LifeHub.Models.Services;

namespace LifeHub.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IFinanceService _financeService;
        private readonly IHabitService _habitService;
        private readonly ILogger<DashboardController> _logger;
        private readonly IDistributedCache _cache;

        public DashboardController(
            UserManager<IdentityUser> userManager,
            ISubscriptionService subscriptionService,
            IFinanceService financeService,
            IHabitService habitService, // ✅ Inyectar HabitService
            ILogger<DashboardController> logger,
            IDistributedCache cache)
        {
            _userManager = userManager;
            _subscriptionService = subscriptionService;
            _financeService = financeService;
            _habitService = habitService;
            _logger = logger;
            _cache = cache;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                _logger.LogInformation("🔄 Cargando dashboard para usuario: {UserId}", user.Id);

                // Obtener suscripción del usuario (forzar recarga desde BD)
                var subscription = await _subscriptionService.GetUserSubscriptionAsync(user.Id);

                if (subscription?.Plan != null)
                {
                    _logger.LogInformation("📊 Plan del usuario: {PlanName}", subscription.Plan.Name);
                    _logger.LogInformation("🏷️ Características del plan - Comunidad: {Community}, Analytics: {Analytics}, AI: {AI}",
                        subscription.Plan.HasCommunityAccess,
                        subscription.Plan.HasAdvancedAnalytics,
                        subscription.Plan.HasAIFeatures);
                    _logger.LogInformation("📈 Límites del plan - Transacciones: {MaxTransactions}, Hábitos: {MaxHabits}, Presupuestos: {MaxBudgets}",
                        subscription.Plan.MaxTransactions,
                        subscription.Plan.MaxHabits,
                        subscription.Plan.MaxBudgets);
                }
                else
                {
                    _logger.LogWarning("⚠️ Usuario sin plan activo");
                }

                // En el método Index, actualiza la sección de salud:
                var healthService = HttpContext.RequestServices.GetService<IHealthService>();
                if (healthService != null)
                {
                    try
                    {
                        var healthStats = await healthService.GetHealthStatsAsync(user.Id);
                        ViewBag.HealthActivitiesCount = healthStats.TotalAppointments;
                        ViewBag.MealsTracked = healthStats.ActiveMedications;
                        ViewBag.DailyHealthGoal = Math.Min((healthStats.TotalAppointments * 10), 100);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "No se pudieron cargar las estadísticas de salud");
                        ViewBag.HealthActivitiesCount = 0;
                        ViewBag.MealsTracked = 0;
                        ViewBag.DailyHealthGoal = 0;
                    }
                }

                // Obtener datos financieros reales
                var financeSummary = await _financeService.GetFinanceSummaryAsync(user.Id);
                _logger.LogInformation("💰 Resumen financiero - Ingresos: {Income}, Gastos: {Expenses}, Transacciones: {Transactions}",
                    financeSummary?.TotalIncome ?? 0,
                    financeSummary?.TotalExpenses ?? 0,
                    financeSummary?.TotalTransactions ?? 0);

                // Obtener transacciones recientes
                var recentTransactions = await GetRecentTransactionsWithFallback(user.Id, _financeService);
                _logger.LogInformation("💳 Transacciones recientes: {Count} transacciones encontradas", recentTransactions?.Count ?? 0);

                // Obtener datos de uso real del plan
                var transactionUsage = await _subscriptionService.GetTransactionUsageAsync(user.Id);
                var budgetUsage = await _subscriptionService.GetBudgetUsageAsync(user.Id);

                _logger.LogInformation("📈 Uso del plan: {TransactionUsage}/{MaxTransactions} transacciones, {BudgetUsage}/{MaxBudgets} presupuestos",
                    transactionUsage.current, transactionUsage.max, budgetUsage.current, budgetUsage.max);

                var hasBudgetAlerts = await _financeService.CheckBudgetAlertsAsync(user.Id);

                // Datos para los módulos
                var activeHabitsCount = 0;
                var currentStreak = 0;
                var weeklyProgress = 0;
                var healthActivitiesCount = 0;
                var mealsTracked = 0;
                var dailyHealthGoal = 0;
                var storageUsedMB = 0;

                // ✅ Obtener estadísticas de hábitos
            var habitStats = await _habitService.GetHabitStatsAsync(user.Id);
            var habitUsage = await _habitService.GetHabitUsageAsync(user.Id);

            _logger.LogInformation("📊 Estadísticas de hábitos - Activos: {ActiveHabits}, Racha: {Streak}", 
                habitStats.ActiveHabits, habitStats.CurrentStreak);

                // Datos de comunidad basados en el plan real
                var communityMembers = subscription?.Plan?.HasCommunityAccess == true ? 128 : 0;
                var activeDiscussions = subscription?.Plan?.HasCommunityAccess == true ? 25 : 0;
                var communityActivity = subscription?.Plan?.HasCommunityAccess == true ? 85 : 0;

                ViewBag.UserEmail = user.Email;
                ViewBag.UserSubscription = subscription;
                ViewBag.FinanceSummary = financeSummary;
                ViewBag.RecentTransactions = recentTransactions;
                ViewBag.HasBudgetAlerts = hasBudgetAlerts;
                ViewBag.ActiveHabitsCount = activeHabitsCount;
                ViewBag.CurrentStreak = currentStreak;
                ViewBag.WeeklyProgress = weeklyProgress;
                ViewBag.HealthActivitiesCount = healthActivitiesCount;
                ViewBag.MealsTracked = mealsTracked;
                ViewBag.DailyHealthGoal = dailyHealthGoal;
                ViewBag.StorageUsedMB = storageUsedMB;
                ViewBag.CommunityMembers = communityMembers;
                ViewBag.ActiveDiscussions = activeDiscussions;
                ViewBag.CommunityActivity = communityActivity;
                ViewBag.TransactionUsage = transactionUsage;
                ViewBag.BudgetUsage = budgetUsage;
                ViewBag.HabitUsage = habitUsage;
                ViewBag.HealthActivitiesCount = 0;
                ViewBag.MealsTracked = 0;
                ViewBag.DailyHealthGoal = 0;
                ViewBag.StorageUsedMB = 0;
                ViewBag.CommunityMembers = subscription?.Plan?.HasCommunityAccess == true ? 128 : 0;
                ViewBag.ActiveDiscussions = subscription?.Plan?.HasCommunityAccess == true ? 25 : 0;
                ViewBag.CommunityActivity = subscription?.Plan?.HasCommunityAccess == true ? 85 : 0;


                _logger.LogInformation("✅ Dashboard cargado exitosamente para usuario: {UserId}", user.Id);

                return View();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "❌ Error al cargar el dashboard para el usuario");
                TempData["ErrorMessage"] = "Error al cargar el dashboard. Por favor, intenta nuevamente.";
                return RedirectToAction("Index", "Home");
            }
        }

        // MÉTODO DE RESPALDO PARA OBTENER TRANSACCIONES
        private async Task<List<FinancialTransaction>> GetRecentTransactionsWithFallback(string userId, IFinanceService financeService)
        {
            try
            {
                _logger.LogInformation("   - 🔄 Obteniendo transacciones recientes...");
                var transactions = await financeService.GetUserTransactionsAsync(userId);

                if (transactions != null && transactions.Any())
                {
                    _logger.LogInformation("   - ✅ Transacciones obtenidas: {Count}", transactions.Count);
                    return transactions.OrderByDescending(t => t.TransactionDate).Take(10).ToList();
                }

                _logger.LogWarning("   - ⚠️ No se encontraron transacciones");
                return new List<FinancialTransaction>();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "   - ❌ Error al obtener transacciones");
                return new List<FinancialTransaction>();
            }
        }

        private async Task LoadHabitStatsAsync()
        {
            var habitService = HttpContext.RequestServices.GetService<IHabitService>();
            if (habitService != null)
            {
                var userId = _userManager.GetUserId(User);
                if (!string.IsNullOrEmpty(userId))
                {
                    var stats = await habitService.GetHabitStatsAsync(userId);
                    ViewBag.ActiveHabitsCount = stats.ActiveHabits;
                    ViewBag.CurrentStreak = stats.CurrentStreak;
                    ViewBag.WeeklyProgress = (int)Math.Min(stats.CompletionRate, 100);
                }
            }
        }
    }
}