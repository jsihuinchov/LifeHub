using LifeHub.Data;
using LifeHub.Models.Entities;
using LifeHub.Models.Services;
using LifeHub.Models.IA.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using LifeHub.Services;
using LifeHub.Models.IA.Results;

namespace LifeHub.Controllers
{
    [Authorize]
    public class FinanceController : Controller
    {
        private readonly IFinanceService _financeService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<FinanceController> _logger;

        public FinanceController(
            IFinanceService financeService,
            ISubscriptionService subscriptionService,
            UserManager<IdentityUser> userManager,
            ILogger<FinanceController> logger)
        {
            _financeService = financeService;
            _subscriptionService = subscriptionService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Finance
        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                // Obtener TODO desde el mismo servicio
                var summary = await _financeService.GetFinanceSummaryAsync(user.Id);
                var recentTransactions = await _financeService.GetUserTransactionsAsync(user.Id);
                var budgetStatus = await _financeService.GetBudgetStatusAsync(user.Id);

                // Gráficos desde el mismo servicio
                var monthlyTrendChart = await _financeService.GetMonthlyTrendChartAsync(user.Id);
                var expenseDistributionChart = await _financeService.GetExpenseDistributionChartAsync(user.Id);
                var incomeDistributionChart = await _financeService.GetIncomeDistributionChartAsync(user.Id);
                var budgetProgressChart = await _financeService.GetBudgetProgressChartAsync(user.Id);

                ViewBag.FinanceSummary = summary;
                ViewBag.RecentTransactions = recentTransactions.Take(8).ToList();
                ViewBag.BudgetStatus = budgetStatus;

                // Pasar gráficos estructurados
                ViewBag.MonthlyTrendChart = monthlyTrendChart ?? new ChartData();
                ViewBag.ExpenseDistributionChart = expenseDistributionChart ?? new ChartData();
                ViewBag.IncomeDistributionChart = incomeDistributionChart ?? new ChartData();
                ViewBag.BudgetProgressChart = budgetProgressChart ?? new ChartData();

                // Información del plan del usuario
                var userSubscription = await _subscriptionService.GetUserSubscriptionAsync(user.Id);
                var userPlan = userSubscription?.Plan;
                var hasAIFeatures = await _subscriptionService.UserHasFeatureAccessAsync(user.Id, "ai");

                ViewBag.UserPlan = userPlan;
                ViewBag.HasAIFeatures = hasAIFeatures;

                // ==== AGREGAR DATOS DE IA ====
                if (hasAIFeatures)
                {
                    // Inyectar el servicio de IA
                    var financeAIService = HttpContext.RequestServices.GetService<IFinanceAIService>();
                    if (financeAIService != null)
                    {
                        var aiAlertsData = await financeAIService.DetectSpendingAnomaliesAsync(user.Id);
                        var spendingPredictionData = await financeAIService.PredictNextMonthSpendingAsync(user.Id);
                        var budgetRecommendationsData = await financeAIService.GenerateBudgetRecommendationsAsync(user.Id);
                        var spendingPatternsData = await financeAIService.AnalyzeSpendingPatternsAsync(user.Id);

                        ViewBag.AIAlerts = aiAlertsData;
                        ViewBag.SpendingPrediction = spendingPredictionData;
                        ViewBag.BudgetRecommendations = budgetRecommendationsData;
                        ViewBag.SpendingPatterns = spendingPatternsData;
                    }
                    else
                    {
                        // Si el servicio no está disponible, usar datos vacíos
                        ViewBag.AIAlerts = new List<FinancialAlert>();
                        ViewBag.SpendingPrediction = 0;
                        ViewBag.BudgetRecommendations = new List<BudgetRecommendation>();
                        ViewBag.SpendingPatterns = new SpendingPatterns();
                    }
                }
                else
                {
                    ViewBag.AIAlerts = new List<FinancialAlert>();
                    ViewBag.SpendingPrediction = 0;
                    ViewBag.BudgetRecommendations = new List<BudgetRecommendation>();
                    ViewBag.SpendingPatterns = new SpendingPatterns();
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar dashboard de finanzas");
                TempData["ErrorMessage"] = "Error al cargar el dashboard financiero.";
                return View();
            }
        }

        // GET: /Finance/Transactions
        public async Task<IActionResult> Transactions(string type, string category, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                var transactions = await _financeService.GetUserTransactionsAsync(user.Id);

                // Aplicar filtros
                if (!string.IsNullOrEmpty(type) && int.TryParse(type, out int transactionType))
                {
                    transactions = transactions.Where(t => t.TransactionType == transactionType).ToList();
                }

                if (!string.IsNullOrEmpty(category))
                {
                    transactions = transactions.Where(t => t.Category != null && t.Category.Contains(category, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (startDate.HasValue)
                {
                    transactions = transactions.Where(t => t.TransactionDate >= startDate.Value.ToUniversalTime()).ToList();
                }

                if (endDate.HasValue)
                {
                    transactions = transactions.Where(t => t.TransactionDate <= endDate.Value.ToUniversalTime()).ToList();
                }

                // Pasar los filtros actuales a la vista
                ViewBag.CurrentType = type;
                ViewBag.CurrentCategory = category;
                ViewBag.CurrentStartDate = startDate?.ToString("yyyy-MM-dd");
                ViewBag.CurrentEndDate = endDate?.ToString("yyyy-MM-dd");

                return View(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar transacciones");
                TempData["ErrorMessage"] = "Error al cargar las transacciones.";
                return View(new List<FinancialTransaction>());
            }
        }

        // GET: /Finance/CreateTransaction
        public IActionResult CreateTransaction()
        {
            return View();
        }

        // POST: /Finance/CreateTransaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTransaction(FinancialTransaction transaction)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(transaction);
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                transaction.UserId = user.Id;
                // El DbContext maneja automáticamente la conversión UTC

                var result = await _financeService.CreateTransactionAsync(transaction);
                if (result)
                {
                    TempData["SuccessMessage"] = "Transacción creada exitosamente.";
                    return RedirectToAction(nameof(Transactions));
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al crear la transacción.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear transacción");
                TempData["ErrorMessage"] = "Error al crear la transacción.";
            }

            return View(transaction);
        }

        // GET: /Finance/EditTransaction/{id}
        public async Task<IActionResult> EditTransaction(int id)
        {
            try
            {
                var transaction = await _financeService.GetTransactionByIdAsync(id);
                if (transaction == null)
                {
                    TempData["ErrorMessage"] = "Transacción no encontrada.";
                    return RedirectToAction(nameof(Transactions));
                }

                var user = await _userManager.GetUserAsync(User);
                if (transaction.UserId != user?.Id)
                {
                    TempData["ErrorMessage"] = "No tienes permiso para editar esta transacción.";
                    return RedirectToAction(nameof(Transactions));
                }

                return View(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar transacción para editar {TransactionId}", id);
                TempData["ErrorMessage"] = "Error al cargar la transacción.";
                return RedirectToAction(nameof(Transactions));
            }
        }

        // POST: /Finance/EditTransaction/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTransaction(int id, FinancialTransaction transaction)
        {
            try
            {
                if (id != transaction.Id)
                {
                    TempData["ErrorMessage"] = "ID de transacción no coincide.";
                    return View(transaction);
                }

                if (!ModelState.IsValid)
                {
                    return View(transaction);
                }

                var user = await _userManager.GetUserAsync(User);
                if (transaction.UserId != user?.Id)
                {
                    TempData["ErrorMessage"] = "No tienes permiso para editar esta transacción.";
                    return RedirectToAction(nameof(Transactions));
                }

                var result = await _financeService.UpdateTransactionAsync(transaction);
                if (result)
                {
                    TempData["SuccessMessage"] = "Transacción actualizada exitosamente.";
                    return RedirectToAction(nameof(Transactions));
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al actualizar la transacción.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar transacción {TransactionId}", id);
                TempData["ErrorMessage"] = "Error al actualizar la transacción.";
            }

            return View(transaction);
        }

        // POST: /Finance/DeleteTransaction/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            try
            {
                var transaction = await _financeService.GetTransactionByIdAsync(id);
                if (transaction == null)
                {
                    TempData["ErrorMessage"] = "Transacción no encontrada.";
                    return RedirectToAction(nameof(Transactions));
                }

                var user = await _userManager.GetUserAsync(User);
                if (transaction.UserId != user?.Id)
                {
                    TempData["ErrorMessage"] = "No tienes permiso para eliminar esta transacción.";
                    return RedirectToAction(nameof(Transactions));
                }

                var result = await _financeService.DeleteTransactionAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Transacción eliminada exitosamente.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al eliminar la transacción.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar transacción {TransactionId}", id);
                TempData["ErrorMessage"] = "Error al eliminar la transacción.";
            }

            return RedirectToAction(nameof(Transactions));
        }

        // GET: /Finance/Budgets
        public async Task<IActionResult> Budgets()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                var budgets = await _financeService.GetUserBudgetsAsync(user.Id);
                var budgetStatus = await _financeService.GetBudgetStatusAsync(user.Id);

                ViewBag.BudgetStatus = budgetStatus;
                return View(budgets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar presupuestos");
                TempData["ErrorMessage"] = "Error al cargar los presupuestos.";
                return View(new List<Budget>());
            }
        }

        // GET: /Finance/CreateBudget
        public IActionResult CreateBudget()
        {
            return View();
        }

        // POST: /Finance/CreateBudget
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBudget(Budget budget)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(budget);
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                budget.UserId = user.Id;
                budget.CurrentAmount = 0;
                // El DbContext maneja automáticamente la conversión UTC

                var result = await _financeService.CreateBudgetAsync(budget);
                if (result)
                {
                    TempData["SuccessMessage"] = "Presupuesto creado exitosamente.";
                    return RedirectToAction(nameof(Budgets));
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al crear el presupuesto.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear presupuesto");
                TempData["ErrorMessage"] = "Error al crear el presupuesto.";
            }

            return View(budget);
        }

        // GET: /Finance/EditBudget/{id}
        public async Task<IActionResult> EditBudget(int id)
        {
            try
            {
                var budget = await _financeService.GetBudgetByIdAsync(id);
                if (budget == null)
                {
                    TempData["ErrorMessage"] = "Presupuesto no encontrado.";
                    return RedirectToAction(nameof(Budgets));
                }

                var user = await _userManager.GetUserAsync(User);
                if (budget.UserId != user?.Id)
                {
                    TempData["ErrorMessage"] = "No tienes permiso para editar este presupuesto.";
                    return RedirectToAction(nameof(Budgets));
                }

                return View(budget);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar presupuesto para editar {BudgetId}", id);
                TempData["ErrorMessage"] = "Error al cargar el presupuesto.";
                return RedirectToAction(nameof(Budgets));
            }
        }

        // POST: /Finance/EditBudget/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBudget(int id, Budget budget)
        {
            try
            {
                if (id != budget.Id)
                {
                    TempData["ErrorMessage"] = "ID de presupuesto no coincide.";
                    return View(budget);
                }

                if (!ModelState.IsValid)
                {
                    return View(budget);
                }

                var user = await _userManager.GetUserAsync(User);
                if (budget.UserId != user?.Id)
                {
                    TempData["ErrorMessage"] = "No tienes permiso para editar este presupuesto.";
                    return RedirectToAction(nameof(Budgets));
                }

                var result = await _financeService.UpdateBudgetAsync(budget);
                if (result)
                {
                    TempData["SuccessMessage"] = "Presupuesto actualizado exitosamente.";
                    return RedirectToAction(nameof(Budgets));
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al actualizar el presupuesto.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar presupuesto {BudgetId}", id);
                TempData["ErrorMessage"] = "Error al actualizar el presupuesto.";
            }

            return View(budget);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBudget(int id)
        {
            _logger.LogInformation("Intentando eliminar presupuesto {BudgetId}", id);

            try
            {
                var budget = await _financeService.GetBudgetByIdAsync(id);
                if (budget == null)
                {
                    _logger.LogWarning("Presupuesto {BudgetId} no encontrado", id);
                    TempData["ErrorMessage"] = "Presupuesto no encontrado.";
                    return RedirectToAction(nameof(Budgets));
                }

                var user = await _userManager.GetUserAsync(User);
                if (budget.UserId != user?.Id)
                {
                    _logger.LogWarning("Usuario no autorizado para eliminar presupuesto {BudgetId}", id);
                    TempData["ErrorMessage"] = "No tienes permiso para eliminar este presupuesto.";
                    return RedirectToAction(nameof(Budgets));
                }

                var result = await _financeService.DeleteBudgetAsync(id);
                if (result)
                {
                    _logger.LogInformation("Presupuesto {BudgetId} eliminado exitosamente", id);
                    TempData["SuccessMessage"] = "Presupuesto eliminado exitosamente.";
                }
                else
                {
                    _logger.LogError("Error al eliminar presupuesto {BudgetId} en el servicio", id);
                    TempData["ErrorMessage"] = "Error al eliminar el presupuesto.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar presupuesto {BudgetId}", id);
                TempData["ErrorMessage"] = "Error al eliminar el presupuesto.";
            }

            return RedirectToAction(nameof(Budgets));
        }

        // GET: /Finance/Reports
        public async Task<IActionResult> Reports()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                var summary = await _financeService.GetFinanceSummaryAsync(user.Id);
                var transactions = await _financeService.GetUserTransactionsAsync(user.Id);
                var categorySummary = await _financeService.GetCategorySummaryAsync(user.Id);
                var expenseByCategory = await _financeService.GetExpenseByCategoryAsync(user.Id);
                var incomeByCategory = await _financeService.GetIncomeByCategoryAsync(user.Id);
                var monthlyData = await _financeService.GetMonthlyDataAsync(user.Id);

                // Obtener datos de gráficos
                var monthlyTrendChart = await _financeService.GetMonthlyTrendChartAsync(user.Id);
                var expenseDistributionChart = await _financeService.GetExpenseDistributionChartAsync(user.Id);

                ViewBag.FinanceSummary = summary;
                ViewBag.Transactions = transactions;
                ViewBag.CategorySummary = categorySummary;
                ViewBag.ExpenseByCategory = expenseByCategory;
                ViewBag.IncomeByCategory = incomeByCategory;
                ViewBag.MonthlyData = monthlyData;

                // Pasar gráficos estructurados
                ViewBag.MonthlyTrendChart = monthlyTrendChart;
                ViewBag.ExpenseDistributionChart = expenseDistributionChart;

                // DEBUG: Log para verificar datos
                _logger.LogInformation("✅ Reportes cargados - Gráfico Tendencia: {TrendLabels}, Gráfico Distribución: {DistLabels}",
                    monthlyTrendChart?.Labels?.Length ?? 0,
                    expenseDistributionChart?.Labels?.Length ?? 0);

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al cargar reportes");
                TempData["ErrorMessage"] = "Error al cargar los reportes.";
                return View();
            }
        }

        // GET: /Finance/GetQuickStats
        public async Task<IActionResult> GetQuickStats()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                var transactions = await _financeService.GetUserTransactionsAsync(user.Id);
                var budgets = await _financeService.GetUserBudgetsAsync(user.Id);
                var summary = await _financeService.GetFinanceSummaryAsync(user.Id);

                return Json(new
                {
                    transactionsCount = transactions.Count,
                    budgetsCount = budgets.Count,
                    balance = summary?.NetAmount ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas rápidas");
                return Json(new { transactionsCount = 0, budgetsCount = 0, balance = 0 });
            }
        }
        // === NUEVOS ENDPOINTS PARA CONSEJOS ===

    // GET: /Finance/Advice/Random
    [HttpGet("Advice/Random")]
    public async Task<IActionResult> GetRandomAdvice()
    {
        try
        {
            var advice = await _financeService.GetRandomAdviceAsync();
            return Ok(advice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener consejo aleatorio");
            return StatusCode(500, "Error al obtener consejo financiero");
        }
    }

    // GET: /Finance/Advice/Daily
    [HttpGet("Advice/Daily")]
    public async Task<IActionResult> GetDailyAdvice()
    {
        try
        {
            var advice = await _financeService.GetDailyAdviceAsync();
            return Ok(advice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener consejo del día");
            return StatusCode(500, "Error al obtener consejo del día");
        }
    }

    // GET: /Finance/Advice/Categories
    [HttpGet("Advice/Categories")]
    public async Task<IActionResult> GetAdviceCategories()
    {
        try
        {
            var categories = await _financeService.GetAdviceCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener categorías de consejos");
            return StatusCode(500, "Error al obtener categorías");
        }
    }

        // GET: /Finance/Advice/Category/{category}
        [HttpGet("Advice/Category/{category}")]
        public async Task<IActionResult> GetAdviceByCategory(string category)
        {
            try
            {
                var adviceList = await _financeService.GetAdviceByCategoryAsync(category);
                return Ok(adviceList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener consejos por categoría {Category}", category);
                return StatusCode(500, $"Error al obtener consejos para {category}");
            }
        }
    }
}