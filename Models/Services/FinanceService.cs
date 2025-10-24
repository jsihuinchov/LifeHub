using LifeHub.Data;
using LifeHub.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace LifeHub.Services
{
    public class FinanceService : IFinanceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<FinanceService> _logger;

        public FinanceService(ApplicationDbContext context, IDistributedCache cache, ILogger<FinanceService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        // Transactions - Versión simplificada
        public async Task<List<FinancialTransaction>> GetUserTransactionsAsync(string userId)
        {
            try
            {
                return await _context.FinancialTransactions
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.TransactionDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener transacciones del usuario {UserId}", userId);
                return new List<FinancialTransaction>();
            }
        }

        // Método adicional con parámetros opcionales para compatibilidad
        public async Task<List<FinancialTransaction>> GetUserTransactionsAsync(string userId, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var query = _context.FinancialTransactions
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.TransactionDate);

                if (startDate.HasValue)
                {
                    var utcStartDate = startDate.Value.ToUniversalTime();
                    query = (IOrderedQueryable<FinancialTransaction>)query.Where(t => t.TransactionDate >= utcStartDate);
                }
                if (endDate.HasValue)
                {
                    var utcEndDate = endDate.Value.ToUniversalTime();
                    query = (IOrderedQueryable<FinancialTransaction>)query.Where(t => t.TransactionDate <= utcEndDate);
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener transacciones del usuario {UserId}", userId);
                return new List<FinancialTransaction>();
            }
        }

        public async Task<FinancialTransaction?> GetTransactionByIdAsync(int id)
        {
            return await _context.FinancialTransactions.FindAsync(id);
        }

        public async Task<bool> CreateTransactionAsync(FinancialTransaction transaction)
        {
            try
            {
                // El DbContext ya maneja la conversión UTC automáticamente
                _context.FinancialTransactions.Add(transaction);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Transacción creada: {TransactionId} para usuario {UserId}", transaction.Id, transaction.UserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear transacción");
                return false;
            }
        }

        public async Task<bool> UpdateTransactionAsync(FinancialTransaction transaction)
        {
            try
            {
                _context.FinancialTransactions.Update(transaction);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar transacción {TransactionId}", transaction.Id);
                return false;
            }
        }

        public async Task<bool> DeleteTransactionAsync(int id)
        {
            try
            {
                var transaction = await _context.FinancialTransactions.FindAsync(id);
                if (transaction == null) return false;

                _context.FinancialTransactions.Remove(transaction);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar transacción {TransactionId}", id);
                return false;
            }
        }

        // Budgets
        public async Task<List<Budget>> GetUserBudgetsAsync(string userId)
        {
            try
            {
                return await _context.Budgets
                    .Where(b => b.UserId == userId)
                    .OrderBy(b => b.Category)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener presupuestos del usuario {UserId}", userId);
                return new List<Budget>();
            }
        }

        public async Task<Budget?> GetBudgetByIdAsync(int id)
        {
            return await _context.Budgets.FindAsync(id);
        }

        public async Task<bool> CreateBudgetAsync(Budget budget)
        {
            try
            {
                _context.Budgets.Add(budget);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear presupuesto");
                return false;
            }
        }

        public async Task<bool> UpdateBudgetAsync(Budget budget)
        {
            try
            {
                _context.Budgets.Update(budget);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar presupuesto {BudgetId}", budget.Id);
                return false;
            }
        }

        public async Task<bool> DeleteBudgetAsync(int id)
        {
            try
            {
                var budget = await _context.Budgets.FindAsync(id);
                if (budget == null) return false;

                _context.Budgets.Remove(budget);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar presupuesto {BudgetId}", id);
                return false;
            }
        }

        // Analytics - Versión principal simplificada
        public async Task<FinanceSummary> GetFinanceSummaryAsync(string userId)
        {
            try
            {
                var transactions = await GetUserTransactionsAsync(userId);
                var currentDate = DateTime.UtcNow;

                // Transacciones del mes actual
                var monthlyTransactions = transactions
                    .Where(t => t.TransactionDate.Month == currentDate.Month &&
                               t.TransactionDate.Year == currentDate.Year)
                    .ToList();

                // Transacciones del mes anterior para calcular crecimiento
                var previousMonthDate = currentDate.AddMonths(-1);
                var previousMonthTransactions = transactions
                    .Where(t => t.TransactionDate.Month == previousMonthDate.Month &&
                               t.TransactionDate.Year == previousMonthDate.Year)
                    .ToList();

                var previousMonthIncome = previousMonthTransactions.Where(t => t.TransactionType == 0).Sum(t => t.Amount);
                var previousMonthExpenses = previousMonthTransactions.Where(t => t.TransactionType == 1).Sum(t => t.Amount);

                var currentMonthIncome = monthlyTransactions.Where(t => t.TransactionType == 0).Sum(t => t.Amount);
                var currentMonthExpenses = monthlyTransactions.Where(t => t.TransactionType == 1).Sum(t => t.Amount);

                var incomeGrowth = previousMonthIncome > 0 ? 
                    ((currentMonthIncome - previousMonthIncome) / previousMonthIncome) * 100 : 0;
                var expenseGrowth = previousMonthExpenses > 0 ? 
                    ((currentMonthExpenses - previousMonthExpenses) / previousMonthExpenses) * 100 : 0;

                var summary = new FinanceSummary
                {
                    TotalIncome = currentMonthIncome,
                    TotalExpenses = currentMonthExpenses,
                    TotalTransactions = monthlyTransactions.Count,
                    AverageTransaction = monthlyTransactions.Count > 0 ? monthlyTransactions.Average(t => t.Amount) : 0,
                    IncomeGrowth = incomeGrowth,
                    ExpenseGrowth = expenseGrowth,
                    MonthlyGoalProgress = CalculateMonthlyGoalProgress(monthlyTransactions)
                };

                // Datos de los últimos 6 meses ORDENADOS correctamente
                for (int i = 5; i >= 0; i--) // Cambiado para orden correcto
                {
                    var monthDate = currentDate.AddMonths(-i);
                    var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var monthTransactions = transactions
                        .Where(t => t.TransactionDate >= monthStart && t.TransactionDate <= monthEnd)
                        .ToList();

                    summary.MonthlyData.Add(new MonthlyData
                    {
                        Month = monthDate.ToString("MMM yyyy"),
                        Income = monthTransactions.Where(t => t.TransactionType == 0).Sum(t => t.Amount),
                        Expenses = monthTransactions.Where(t => t.TransactionType == 1).Sum(t => t.Amount),
                        Savings = monthTransactions.Where(t => t.TransactionType == 0).Sum(t => t.Amount) -
                                 monthTransactions.Where(t => t.TransactionType == 1).Sum(t => t.Amount)
                    });
                }

                // Top categorías de GASTOS
                var expenseCategories = monthlyTransactions
                    .Where(t => t.TransactionType == 1 && !string.IsNullOrEmpty(t.Category))
                    .GroupBy(t => t.Category!)
                    .Select(g => new CategoryData
                    {
                        Category = g.Key,
                        Amount = g.Sum(t => t.Amount),
                        Percentage = summary.TotalExpenses > 0 ? (g.Sum(t => t.Amount) / summary.TotalExpenses) * 100 : 0
                    })
                    .OrderByDescending(c => c.Amount)
                    .Take(5)
                    .ToList();

                summary.TopExpenseCategories = expenseCategories;

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen financiero para usuario {UserId}", userId);
                return new FinanceSummary();
            }
        }

        // Método adicional con parámetro de meses para compatibilidad
        public async Task<FinanceSummary> GetFinanceSummaryAsync(string userId, int months)
        {
            return await GetFinanceSummaryAsync(userId); // Usamos la implementación simplificada
        }

        // NUEVOS MÉTODOS PARA EL DASHBOARD MEJORADO
        public async Task<List<MonthlyData>> GetMonthlyDataAsync(string userId, int months = 6)
        {
            try
            {
                _logger.LogInformation("📅 Obteniendo datos mensuales para usuario {UserId} - últimos {Months} meses", userId, months);

                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddMonths(-months);

                _logger.LogInformation("Rango de fechas: {StartDate} a {EndDate}", startDate, endDate);

                // Obtener transacciones del rango
                var transactions = await _context.FinancialTransactions
                    .Where(t => t.UserId == userId && t.TransactionDate >= startDate)
                    .ToListAsync();

                _logger.LogInformation("Transacciones encontradas: {Count}", transactions.Count);

                // Generar todos los meses del rango
                var monthlyData = new List<MonthlyData>();
                var current = startDate;

                while (current <= endDate)
                {
                    var monthStart = new DateTime(current.Year, current.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var monthTransactions = transactions
                        .Where(t => t.TransactionDate >= monthStart && t.TransactionDate <= monthEnd)
                        .ToList();

                    var income = monthTransactions.Where(t => t.TransactionType == 0).Sum(t => t.Amount);
                    var expenses = monthTransactions.Where(t => t.TransactionType == 1).Sum(t => t.Amount);
                    var savings = income - expenses;

                    monthlyData.Add(new MonthlyData
                    {
                        Month = monthStart.ToString("MMM yyyy"),
                        Income = income,
                        Expenses = expenses,
                        Savings = savings
                    });

                    _logger.LogInformation("Mes {Month}: Ingresos={Income}, Gastos={Expenses}, Ahorro={Savings}",
                        monthStart.ToString("MMM yyyy"), income, expenses, savings);

                    current = current.AddMonths(1);
                }

                _logger.LogInformation("Datos mensuales generados: {Count} meses", monthlyData.Count);
                return monthlyData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al obtener datos mensuales para usuario {UserId}", userId);
                return new List<MonthlyData>();
            }
        }

        public async Task<List<CategoryData>> GetExpenseByCategoryAsync(string userId)
        {
            try
            {
                var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                var expenses = await _context.FinancialTransactions
                    .Where(t => t.UserId == userId &&
                               t.TransactionType == 1 &&
                               t.TransactionDate >= startOfMonth)
                    .GroupBy(t => t.Category ?? "Sin categoría")
                    .Select(g => new
                    {
                        Category = g.Key,
                        Amount = g.Sum(t => t.Amount)
                    })
                    .ToListAsync();

                var totalExpenses = expenses.Sum(e => e.Amount);

                var result = expenses.Select(e => new CategoryData
                {
                    Category = e.Category,
                    Amount = e.Amount,
                    Percentage = totalExpenses > 0 ? (e.Amount / totalExpenses) * 100 : 0
                })
                .OrderByDescending(e => e.Amount)
                .ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener gastos por categoría para usuario {UserId}", userId);
                return new List<CategoryData>();
            }
        }   

        public async Task<List<CategoryData>> GetIncomeByCategoryAsync(string userId)
        {
            try
            {
                var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                var incomes = await _context.FinancialTransactions
                    .Where(t => t.UserId == userId &&
                               t.TransactionType == 0 &&
                               t.TransactionDate >= startOfMonth)
                    .GroupBy(t => t.Category ?? "Sin categoría")
                    .Select(g => new
                    {
                        Category = g.Key,
                        Amount = g.Sum(t => t.Amount)
                    })
                    .ToListAsync();

                var totalIncome = incomes.Sum(i => i.Amount);

                var result = incomes.Select(i => new CategoryData
                {
                    Category = i.Category,
                    Amount = i.Amount,
                    Percentage = totalIncome > 0 ? (i.Amount / totalIncome) * 100 : 0
                })
                .OrderByDescending(i => i.Amount)
                .ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ingresos por categoría para usuario {UserId}", userId);
                return new List<CategoryData>();
            }
        }

        // Métodos adicionales para compatibilidad
        public async Task<List<CategorySummary>> GetCategorySummaryAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // Obtener transacciones con filtros de fecha
                var query = _context.FinancialTransactions
                    .Where(t => t.UserId == userId);

                if (startDate.HasValue)
                {
                    var utcStartDate = startDate.Value.ToUniversalTime();
                    query = query.Where(t => t.TransactionDate >= utcStartDate);
                }

                if (endDate.HasValue)
                {
                    var utcEndDate = endDate.Value.ToUniversalTime();
                    query = query.Where(t => t.TransactionDate <= utcEndDate);
                }

                var transactions = await query.ToListAsync();

                // Agrupar por categoría
                var categoryData = transactions
                    .Where(t => !string.IsNullOrEmpty(t.Category))
                    .GroupBy(t => t.Category!)
                    .Select(g => new CategorySummary
                    {
                        Category = g.Key,
                        TotalAmount = g.Sum(t => t.Amount),
                        TransactionCount = g.Count(),
                        AverageAmount = g.Average(t => t.Amount)
                    })
                    .OrderByDescending(c => c.TotalAmount)
                    .ToList();

                return categoryData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen de categorías para usuario {UserId}", userId);
                return new List<CategorySummary>();
            }
        }

        public async Task<MonthlySummary> GetMonthlySummaryAsync(string userId, int year, int month)
        {
            try
            {
                var startDate = new DateTime(year, month, 1).ToUniversalTime();
                var endDate = startDate.AddMonths(1).AddDays(-1).ToUniversalTime();

                var transactions = await _context.FinancialTransactions
                    .Where(t => t.UserId == userId && t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                    .ToListAsync();

                var summary = new MonthlySummary
                {
                    Year = year,
                    Month = month,
                    TotalIncome = transactions.Where(t => t.TransactionType == 0).Sum(t => t.Amount),
                    TotalExpenses = transactions.Where(t => t.TransactionType == 1).Sum(t => t.Amount)
                };

                summary.CategoryBreakdown = await GetCategorySummaryAsync(userId, startDate, endDate);

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen mensual para usuario {UserId}", userId);
                return new MonthlySummary();
            }
        }

        public async Task<List<BudgetStatus>> GetBudgetStatusAsync(string userId)
        {
            try
            {
                var budgets = await GetUserBudgetsAsync(userId);
                var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
                var currentBudgets = budgets.Where(b => b.MonthYear == currentMonth).ToList();

                var statusList = new List<BudgetStatus>();

                // Obtener transacciones del mes actual una sola vez para optimizar
                var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var monthlyTransactions = await _context.FinancialTransactions
                    .Where(t => t.UserId == userId &&
                               t.TransactionDate >= startOfMonth &&
                               t.TransactionDate <= endOfMonth)
                    .ToListAsync();

                foreach (var budget in currentBudgets)
                {
                    decimal spentAmount = 0;

                    if (budget.BudgetType == 1) // Gastos
                    {
                        // PARA GASTOS: Sumar solo los gastos de la categoría específica
                        spentAmount = monthlyTransactions
                            .Where(t => t.TransactionType == 1 && // Solo gastos
                                       t.Category == budget.Category) // Misma categoría
                            .Sum(t => t.Amount);
                    }
                    else if (budget.BudgetType == 0) // AHORRO
                    {
                        // PARA AHORRO: Calcular diferencia entre ingresos y gastos DE ESA CATEGORÍA ESPECÍFICA
                        var categoryIncome = monthlyTransactions
                            .Where(t => t.TransactionType == 0 && // Solo ingresos
                                       t.Category == budget.Category) // Misma categoría
                            .Sum(t => t.Amount);

                        var categoryExpenses = monthlyTransactions
                            .Where(t => t.TransactionType == 1 && // Solo gastos
                                       t.Category == budget.Category) // Misma categoría
                            .Sum(t => t.Amount);

                        var actualSavings = categoryIncome - categoryExpenses;
                        spentAmount = Math.Max(0, actualSavings); // El ahorro no puede ser negativo
                    }

                    statusList.Add(new BudgetStatus
                    {
                        Budget = budget,
                        SpentAmount = spentAmount
                    });
                }

                return statusList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estado de presupuestos para usuario {UserId}", userId);
                return new List<BudgetStatus>();
            }
        }

        public async Task<bool> CheckBudgetAlertsAsync(string userId)
        {
            try
            {
                var budgetStatus = await GetBudgetStatusAsync(userId);
                return budgetStatus.Any(bs => bs.UsagePercentage >= 80);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar alertas de presupuesto para usuario {UserId}", userId);
                return false;
            }
        }

        public async Task<DetailedFinanceReport> GetDetailedFinanceReportAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var transactions = await GetUserTransactionsAsync(userId);

                // Aplicar filtros de fecha
                if (startDate.HasValue)
                {
                    var utcStartDate = startDate.Value.ToUniversalTime();
                    transactions = transactions.Where(t => t.TransactionDate >= utcStartDate).ToList();
                }

                if (endDate.HasValue)
                {
                    var utcEndDate = endDate.Value.ToUniversalTime();
                    transactions = transactions.Where(t => t.TransactionDate <= utcEndDate).ToList();
                }

                var report = new DetailedFinanceReport
                {
                    PeriodStart = startDate ?? transactions.Min(t => t.TransactionDate),
                    PeriodEnd = endDate ?? transactions.Max(t => t.TransactionDate),
                    TotalTransactions = transactions.Count,
                    TotalIncome = transactions.Where(t => t.TransactionType == 0).Sum(t => t.Amount),
                    TotalExpenses = transactions.Where(t => t.TransactionType == 1).Sum(t => t.Amount),
                    NetSavings = transactions.Where(t => t.TransactionType == 0).Sum(t => t.Amount) -
                                transactions.Where(t => t.TransactionType == 1).Sum(t => t.Amount)
                };

                // Análisis por categoría (Ingresos y Gastos)
                report.IncomeByCategory = transactions
                    .Where(t => t.TransactionType == 0 && !string.IsNullOrEmpty(t.Category))
                    .GroupBy(t => t.Category!)
                    .Select(g => new CategoryAnalysis
                    {
                        Category = g.Key,
                        TotalAmount = g.Sum(t => t.Amount),
                        TransactionCount = g.Count(),
                        AverageAmount = g.Average(t => t.Amount),
                        Percentage = report.TotalIncome > 0 ? (g.Sum(t => t.Amount) / report.TotalIncome) * 100 : 0
                    })
                    .OrderByDescending(c => c.TotalAmount)
                    .ToList();

                report.ExpensesByCategory = transactions
                    .Where(t => t.TransactionType == 1 && !string.IsNullOrEmpty(t.Category))
                    .GroupBy(t => t.Category!)
                    .Select(g => new CategoryAnalysis
                    {
                        Category = g.Key,
                        TotalAmount = g.Sum(t => t.Amount),
                        TransactionCount = g.Count(),
                        AverageAmount = g.Average(t => t.Amount),
                        Percentage = report.TotalExpenses > 0 ? (g.Sum(t => t.Amount) / report.TotalExpenses) * 100 : 0
                    })
                    .OrderByDescending(c => c.TotalAmount)
                    .ToList();

                // Análisis mensual
                var allMonths = transactions
                    .Select(t => new DateTime(t.TransactionDate.Year, t.TransactionDate.Month, 1))
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                foreach (var month in allMonths)
                {
                    var monthTransactions = transactions
                        .Where(t => t.TransactionDate.Year == month.Year && t.TransactionDate.Month == month.Month)
                        .ToList();

                    report.MonthlyAnalysis.Add(new MonthlyAnalysis
                    {
                        Month = month,
                        Income = monthTransactions.Where(t => t.TransactionType == 0).Sum(t => t.Amount),
                        Expenses = monthTransactions.Where(t => t.TransactionType == 1).Sum(t => t.Amount),
                        Savings = monthTransactions.Where(t => t.TransactionType == 0).Sum(t => t.Amount) -
                                 monthTransactions.Where(t => t.TransactionType == 1).Sum(t => t.Amount),
                        TransactionCount = monthTransactions.Count
                    });
                }

                // Transacciones más grandes
                report.LargestIncome = transactions
                    .Where(t => t.TransactionType == 0)
                    .OrderByDescending(t => t.Amount)
                    .Take(5)
                    .ToList();

                report.LargestExpenses = transactions
                    .Where(t => t.TransactionType == 1)
                    .OrderByDescending(t => t.Amount)
                    .Take(5)
                    .ToList();

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte detallado para usuario {UserId}", userId);
                return new DetailedFinanceReport();
            }
        }

        // Método auxiliar para calcular progreso de metas mensuales
        private decimal CalculateMonthlyGoalProgress(List<FinancialTransaction> monthlyTransactions)
        {
            // Simulación de cálculo de progreso de metas
            // En una implementación real, esto vendría de configuraciones del usuario
            var income = monthlyTransactions.Where(t => t.TransactionType == 0).Sum(t => t.Amount);
            var expenses = monthlyTransactions.Where(t => t.TransactionType == 1).Sum(t => t.Amount);

            // Meta hipotética: ahorrar el 20% de los ingresos
            var savingsGoal = income * 0.2m;
            var actualSavings = income - expenses;

            if (savingsGoal <= 0) return 0;

            var progress = (actualSavings / savingsGoal) * 100;
            return Math.Min(progress, 100); // Máximo 100%
        }
        
        // ... (todos tus métodos existentes se mantienen igual) ...

        // === NUEVOS MÉTODOS PARA CONSEJOS FINANCIEROS ===
        
        public async Task<FinancialAdvice> GetRandomAdviceAsync()
        {
            var adviceList = await GetFinancialAdviceListAsync();
            var random = new Random();
            return adviceList[random.Next(adviceList.Count)];
        }

        public async Task<FinancialAdvice> GetDailyAdviceAsync()
        {
            var adviceList = await GetFinancialAdviceListAsync();
            var today = DateTime.Today;
            var seed = today.Year * 10000 + today.Month * 100 + today.Day;
            var random = new Random(seed);
            
            return adviceList[random.Next(adviceList.Count)];
        }

        public async Task<List<FinancialAdvice>> GetAdviceByCategoryAsync(string category)
        {
            var adviceList = await GetFinancialAdviceListAsync();
            return adviceList.Where(a => a.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<List<string>> GetAdviceCategoriesAsync()
        {
            var adviceList = await GetFinancialAdviceListAsync();
            return adviceList.Select(a => a.Category).Distinct().ToList();
        }

        private async Task<List<FinancialAdvice>> GetFinancialAdviceListAsync()
        {
            // Base de datos local de consejos - 55+ consejos
            return new List<FinancialAdvice>
            {
                // 📊 CATEGORÍA: AHORRO (15 consejos)
                new() { Id = 1, Category = "Ahorro", Difficulty = "Beginner", Advice = "💡 Automatiza tus ahorros: configura transferencias automáticas del 10% de tus ingresos a una cuenta de ahorros" },
                new() { Id = 2, Category = "Ahorro", Difficulty = "Beginner", Advice = "💰 La regla 50-30-20: destina 50% a necesidades, 30% a deseos y 20% al ahorro" },
                new() { Id = 3, Category = "Ahorro", Difficulty = "Intermediate", Advice = "🎯 Establece metas SMART: Específicas, Medibles, Alcanzables, Relevantes y con Tiempo definido" },
                new() { Id = 4, Category = "Ahorro", Difficulty = "Beginner", Advice = "📱 Usa la regla de las 24 horas: espera un día antes de compras no esenciales para evitar impulsos" },
                new() { Id = 5, Category = "Ahorro", Difficulty = "Intermediate", Advice = "🏦 Crea fondos separados: emergencia (3-6 meses de gastos), vacaciones, regalos, salud" },
                new() { Id = 6, Category = "Ahorro", Difficulty = "Advanced", Advice = "📈 Ahorra los aumentos de sueldo: destina el 50% de cualquier aumento a tus ahorros" },
                new() { Id = 7, Category = "Ahorro", Difficulty = "Beginner", Advice = "🔄 Redondea tus gastos: redondea cada compra al siguiente 10 y ahorra la diferencia" },
                new() { Id = 8, Category = "Ahorro", Difficulty = "Intermediate", Advice = "🎁 Aprovecha los reembolsos: guarda cualquier reembolso o dinero inesperado directamente en ahorros" },
                new() { Id = 9, Category = "Ahorro", Difficulty = "Beginner", Advice = "📊 Revisa suscripciones: cancela al menos una suscripción que no uses mensualmente" },
                new() { Id = 10, Category = "Ahorro", Difficulty = "Intermediate", Advice = "🍽️ Cocina en casa: preparar comida 2 veces más por semana puede ahorrarte hasta 30% en alimentación" },
                new() { Id = 11, Category = "Ahorro", Difficulty = "Advanced", Advice = "🚗 Transporte inteligente: usa transporte público 2 días por semana, ahorrarás en gasolina y mantenimiento" },
                new() { Id = 12, Category = "Ahorro", Difficulty = "Beginner", Advice = "🎯 Ahorro por objetivos: divide metas grandes en mini-metas mensuales más alcanzables" },
                new() { Id = 13, Category = "Ahorro", Difficulty = "Intermediate", Advice = "🏠 Reduce costos de vivienda: considera roommates o alquilar un espacio más pequeño si es posible" },
                new() { Id = 14, Category = "Ahorro", Difficulty = "Beginner", Advice = "📱 Usa apps de cashback: obtén reembolsos en tus compras habituales sin esfuerzo adicional" },
                new() { Id = 15, Category = "Ahorro", Difficulty = "Advanced", Advice = "💡 Ahorro por desafío: prueba el desafío de 52 semanas, ahorrando $1 la semana 1, $2 la semana 2, etc." },

                // 💳 CATEGORÍA: GASTOS (15 consejos)
                new() { Id = 16, Category = "Gastos", Difficulty = "Beginner", Advice = "📝 Registra todos tus gastos por 30 días: el simple acto de anotar reduce gastos innecesarios en 15%" },
                new() { Id = 17, Category = "Gastos", Difficulty = "Beginner", Advice = "🛒 Haz listas de compras: compra solo lo planeado y evita compras impulsivas" },
                new() { Id = 18, Category = "Gastos", Difficulty = "Intermediate", Advice = "📊 Analiza patrones: revisa tus gastos cada domingo para identificar tendencias y ajustar" },
                new() { Id = 19, Category = "Gastos", Difficulty = "Beginner", Advice = "❔ Pregunta '¿Lo necesito o lo quiero?': esta simple pregunta puede reducir gastos impulsivos hasta 40%" },
                new() { Id = 20, Category = "Gastos", Difficulty = "Intermediate", Advice = "🎯 Establece límites por categoría: asigna montos máximos para entretenimiento, comida fuera, etc." },
                new() { Id = 21, Category = "Gastos", Difficulty = "Advanced", Advice = "📱 Usa solo efectivo para gastos discrecionales: el dolor de pagar en efectivo reduce gastos en 20%" },
                new() { Id = 22, Category = "Gastos", Difficulty = "Beginner", Advice = "🕒 Evita compras nocturnas: la mayoría de las compras impulsivas ocurren después de las 8 PM" },
                new() { Id = 23, Category = "Gastos", Difficulty = "Intermediate", Advice = "📦 Espera 30 días para compras grandes: si todavía lo quieres después de un mes, considéralo seriamente" },
                new() { Id = 24, Category = "Gastos", Difficulty = "Beginner", Advice = "🍽️ Come antes de comprar: ir al supermercado con hambre aumenta los gastos en 25%" },
                new() { Id = 25, Category = "Gastos", Difficulty = "Intermediate", Advice = "🔍 Compara precios: siempre revisa al menos 3 opciones antes de compras importantes" },
                new() { Id = 26, Category = "Gastos", Difficulty = "Advanced", Advice = "📅 Compra en temporada baja: viajes, ropa y electrónicos tienen mejores precios en temporadas específicas" },
                new() { Id = 27, Category = "Gastos", Difficulty = "Beginner", Advice = "💡 Usa iluminación LED: reduce tu factura de electricidad hasta 80% en iluminación" },
                new() { Id = 28, Category = "Gastos", Difficulty = "Intermediate", Advice = "🚰 Reduce consumo de agua: instala aireadores en grifos y toma duchas más cortas" },
                new() { Id = 29, Category = "Gastos", Difficulty = "Beginner", Advice = "📱 Desactiva notificaciones de compras: menos tentación, menos gastos impulsivos" },
                new() { Id = 30, Category = "Gastos", Difficulty = "Advanced", Advice = "🏠 Optimiza servicios del hogar: revisa seguros, planes de teléfono y TV cada 6 meses" },

                // 📈 CATEGORÍA: INVERSIONES (10 consejos)
                new() { Id = 31, Category = "Inversiones", Difficulty = "Beginner", Advice = "⏰ Comienza temprano: gracias al interés compuesto, empezar a los 25 vs 35 puede duplicar tu patrimonio" },
                new() { Id = 32, Category = "Inversiones", Difficulty = "Beginner", Advice = "📊 Diversifica: no pongas todos tus huevos en la misma canasta, distribuye tu riesgo" },
                new() { Id = 33, Category = "Inversiones", Difficulty = "Intermediate", Advice = "🔄 Inversión automática: configura aportes automáticos a fondos indexados cada mes" },
                new() { Id = 34, Category = "Inversiones", Difficulty = "Advanced", Advice = "📉 Compra cuando hay miedo: los mejores momentos para invertir son cuando otros tienen pánico" },
                new() { Id = 35, Category = "Inversiones", Difficulty = "Beginner", Advice = "🎯 Enfócate en el largo plazo: el tiempo en el mercado supera al timing del mercado" },
                new() { Id = 36, Category = "Inversiones", Difficulty = "Intermediate", Advice = "💰 Reinvierte dividendos: el interés compuesto es la octava maravilla del mundo" },
                new() { Id = 37, Category = "Inversiones", Difficulty = "Beginner", Advice = "📚 Edúcate constantemente: dedica 1 hora semanal a aprender sobre finanzas personales" },
                new() { Id = 38, Category = "Inversiones", Difficulty = "Advanced", Advice = "🏦 Minimiza comisiones: incluso 1% de comisión anual puede reducir tu patrimonio final en 30%" },
                new() { Id = 39, Category = "Inversiones", Difficulty = "Intermediate", Advice = "📅 Dollar-cost averaging: invierte la misma cantidad regularmente sin importar las condiciones del mercado" },
                new() { Id = 40, Category = "Inversiones", Difficulty = "Beginner", Advice = "🛡️ Primero fondos de emergencia: antes de invertir, asegura 3-6 meses de gastos esenciales" },

                // 🎯 CATEGORÍA: METAS Y PLANIFICACIÓN (10 consejos)
                new() { Id = 41, Category = "Metas", Difficulty = "Beginner", Advice = "📝 Escribe tus metas: las personas que escriben sus metas tienen 42% más probabilidad de lograrlas" },
                new() { Id = 42, Category = "Metas", Difficulty = "Intermediate", Advice = "🎯 Divide y vencerás: divide metas grandes en hitos mensuales y celebra cada logro" },
                new() { Id = 43, Category = "Metas", Difficulty = "Beginner", Advice = "📊 Revisa progreso semanal: 15 minutos cada domingo para ajustar tu plan según el progreso" },
                new() { Id = 44, Category = "Metas", Difficulty = "Advanced", Advice = "🔄 Planificación por escenarios: prepara planes A, B y C para diferentes situaciones económicas" },
                new() { Id = 45, Category = "Metas", Difficulty = "Intermediate", Advice = "💰 Automatiza tus metas: configura transferencias automáticas hacia cada meta específica" },
                new() { Id = 46, Category = "Metas", Difficulty = "Beginner", Advice = "🎉 Recompénsate: celebra los hitos alcanzados con recompensas que no saboteen tu progreso" },
                new() { Id = 47, Category = "Metas", Difficulty = "Intermediate", Advice = "📱 Usa visualizaciones: gráficos y progreso visual aumentan la motivación en 35%" },
                new() { Id = 48, Category = "Metas", Difficulty = "Advanced", Advice = "🔄 Revisión trimestral: cada 3 meses, evalúa si tus metas siguen alineadas con tus prioridades" },
                new() { Id = 49, Category = "Metas", Difficulty = "Beginner", Advice = "🤝 Comparte tus metas: contárselo a alguien aumenta tu compromiso y accountability" },
                new() { Id = 50, Category = "Metas", Difficulty = "Intermediate", Advice = "📈 Ajusta por inflación: considera aumentos de precios al planificar metas a largo plazo" },

                // 🧠 CATEGORÍA: PSICOLOGÍA FINANCIERA (5 consejos)
                new() { Id = 51, Category = "Psicología", Difficulty = "Beginner", Advice = "🎭 Identifica tus triggers emocionales: ¿compras por estrés, aburrimiento o felicidad?" },
                new() { Id = 52, Category = "Psicología", Difficulty = "Intermediate", Advice = "🔄 Cambia tu mentalidad: de 'no puedo gastar' a 'elijo ahorrar para...'" },
                new() { Id = 53, Category = "Psicología", Difficulty = "Beginner", Advice = "📚 Aprende de los errores: cada 'desliz' financiero es una oportunidad de aprendizaje" },
                new() { Id = 54, Category = "Psicología", Difficulty = "Advanced", Advice = "🧘 Practica mindfulness financiero: 5 minutos diarios de conciencia sobre tus hábitos de gasto" },
                new() { Id = 55, Category = "Psicología", Difficulty = "Intermediate", Advice = "🎯 Enfócate en el progreso, no en la perfección: pequeños pasos consistentes crean grandes resultados" }
            };
        }

        // === NUEVA SECCIÓN: MÉTODOS DE GRÁFICOS ===
        public async Task<ChartData> GetMonthlyTrendChartAsync(string userId, int months = 6)
        {
            try
            {
                _logger.LogInformation("📊 Generando gráfico de tendencia mensual para usuario {UserId}", userId);

                var monthlyData = await GetMonthlyDataAsync(userId, months);

                // Si no hay datos, crear estructura básica pero válida
                if (monthlyData == null || !monthlyData.Any() || monthlyData.All(m => m.Income == 0 && m.Expenses == 0))
                {
                    _logger.LogWarning("No hay datos mensuales para el gráfico de tendencia, usando datos de ejemplo");

                    // Crear datos de ejemplo para los últimos 6 meses
                    var exampleData = new List<MonthlyData>();
                    var now = DateTime.UtcNow;

                    for (int i = 5; i >= 0; i--)
                    {
                        var date = now.AddMonths(-i);
                        exampleData.Add(new MonthlyData
                        {
                            Month = date.ToString("MMM yyyy"),
                            Income = 3000 + (i * 200),
                            Expenses = 2500 + (i * 150),
                            Savings = 500 + (i * 50)
                        });
                    }

                    monthlyData = exampleData;
                }

                _logger.LogInformation("Datos procesados: {Count} meses", monthlyData.Count);

                return new ChartData
                {
                    Type = "line",
                    Labels = monthlyData.Select(m => m.Month).ToArray(),
                    Datasets = new List<ChartDataset>
            {
                new ChartDataset
                {
                    Label = "Ingresos",
                    Data = monthlyData.Select(m => m.Income).ToArray(),
                    BorderColor = new[] { "#10b981" },
                    BackgroundColor = new[] { "rgba(16, 185, 129, 0.1)" },
                    BorderWidth = 3,
                    Fill = true,
                    Tension = 0.4m
                },
                new ChartDataset
                {
                    Label = "Gastos",
                    Data = monthlyData.Select(m => m.Expenses).ToArray(),
                    BorderColor = new[] { "#ef4444" },
                    BackgroundColor = new[] { "rgba(239, 68, 68, 0.1)" },
                    BorderWidth = 3,
                    Fill = true,
                    Tension = 0.4m
                },
                new ChartDataset
                {
                    Label = "Ahorro",
                    Data = monthlyData.Select(m => m.Savings).ToArray(),
                    BorderColor = new[] { "#3b82f6" },
                    BackgroundColor = new[] { "rgba(59, 130, 246, 0.1)" },
                    BorderWidth = 2,
                    Fill = false,
                    Tension = 0.4m,
                    BorderDash = new[] { 5, 5 }
                }
            }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al generar gráfico de tendencia mensual para usuario {UserId}", userId);

                // Devolver estructura básica de error
                return new ChartData
                {
                    Type = "line",
                    Labels = new[] { "Error" },
                    Datasets = new List<ChartDataset>
            {
                new ChartDataset
                {
                    Label = "Error",
                    Data = new decimal[] { 0 }
                }
            }
                };
            }
        }

        public async Task<ChartData> GetExpenseDistributionChartAsync(string userId)
        {
            try
            {
                _logger.LogInformation("📊 Generando gráfico de distribución de gastos para usuario {UserId}", userId);

                var expensesByCategory = await GetExpenseByCategoryAsync(userId);

                // Si no hay datos, crear datos de ejemplo
                if (expensesByCategory == null || !expensesByCategory.Any() || expensesByCategory.All(e => e.Amount == 0))
                {
                    _logger.LogWarning("No hay datos de gastos por categoría, usando datos de ejemplo");

                    expensesByCategory = new List<CategoryData>
            {
                new CategoryData { Category = "Alimentación", Amount = 800, Percentage = 32 },
                new CategoryData { Category = "Vivienda", Amount = 1200, Percentage = 48 },
                new CategoryData { Category = "Transporte", Amount = 300, Percentage = 12 },
                new CategoryData { Category = "Entretenimiento", Amount = 150, Percentage = 6 },
                new CategoryData { Category = "Otros", Amount = 50, Percentage = 2 }
            };
                }

                _logger.LogInformation("Categorías de gastos procesadas: {Count}", expensesByCategory.Count);

                // Usar colores consistentes
                var colors = new List<string>
        {
            "#ef4444", "#f59e0b", "#10b981", "#8b5cf6", "#ec4899",
            "#06b6d4", "#84cc16", "#3b82f6", "#f97316", "#64748b"
        };

                return new ChartData
                {
                    Type = "doughnut",
                    Labels = expensesByCategory.Select(e => e.Category).ToArray(),
                    Datasets = new List<ChartDataset>
            {
                new ChartDataset
                {
                    Data = expensesByCategory.Select(e => e.Amount).ToArray(),
                    BackgroundColor = expensesByCategory.Select((e, index) =>
                        colors[index % colors.Count]).ToArray(),
                    BorderColor = expensesByCategory.Select(_ => "#ffffff").ToArray(),
                    BorderWidth = 2
                }
            }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al generar gráfico de distribución de gastos para usuario {UserId}", userId);

                return new ChartData
                {
                    Type = "doughnut",
                    Labels = new[] { "Error" },
                    Datasets = new List<ChartDataset>
            {
                new ChartDataset
                {
                    Data = new decimal[] { 1 },
                    BackgroundColor = new[] { "#ef4444" }
                }
            }
                };
            }
        }

        public async Task<ChartData> GetIncomeDistributionChartAsync(string userId)
        {
            try
            {
                _logger.LogInformation("📊 Generando gráfico de distribución de ingresos para usuario {UserId}", userId);

                var incomeByCategory = await GetIncomeByCategoryAsync(userId);

                // Si no hay datos, crear datos de ejemplo
                if (incomeByCategory == null || !incomeByCategory.Any() || incomeByCategory.All(i => i.Amount == 0))
                {
                    _logger.LogWarning("No hay datos de ingresos por categoría, usando datos de ejemplo");

                    incomeByCategory = new List<CategoryData>
            {
                new CategoryData { Category = "Salario", Amount = 3500, Percentage = 87.5m },
                new CategoryData { Category = "Freelance", Amount = 300, Percentage = 7.5m },
                new CategoryData { Category = "Inversiones", Amount = 200, Percentage = 5 }
            };
                }

                _logger.LogInformation("Categorías de ingresos procesadas: {Count}", incomeByCategory.Count);

                var colors = new List<string>
        {
            "#10b981", "#84cc16", "#8b5cf6", "#06b6d4", "#3b82f6"
        };

                return new ChartData
                {
                    Type = "doughnut",
                    Labels = incomeByCategory.Select(i => i.Category).ToArray(),
                    Datasets = new List<ChartDataset>
            {
                new ChartDataset
                {
                    Data = incomeByCategory.Select(i => i.Amount).ToArray(),
                    BackgroundColor = incomeByCategory.Select((i, index) =>
                        colors[index % colors.Count]).ToArray(),
                    BorderColor = incomeByCategory.Select(_ => "#ffffff").ToArray(),
                    BorderWidth = 2
                }
            }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al generar gráfico de distribución de ingresos para usuario {UserId}", userId);

                return new ChartData
                {
                    Type = "doughnut",
                    Labels = new[] { "Error" },
                    Datasets = new List<ChartDataset>
            {
                new ChartDataset
                {
                    Data = new decimal[] { 1 },
                    BackgroundColor = new[] { "#10b981" }
                }
            }
                };
            }
        }

        public async Task<ChartData> GetBudgetProgressChartAsync(string userId)
        {
            try
            {
                _logger.LogInformation("📊 Generando gráfico de progreso de presupuestos para usuario {UserId}", userId);

                var budgetStatus = await GetBudgetStatusAsync(userId);

                // Si no hay presupuestos, crear datos de ejemplo
                if (budgetStatus == null || !budgetStatus.Any())
                {
                    _logger.LogWarning("No hay datos de presupuestos, usando datos de ejemplo");

                    budgetStatus = new List<BudgetStatus>
            {
                new BudgetStatus
                {
                    Budget = new Budget { Category = "Alimentación", MonthlyAmount = 800 },
                    SpentAmount = 650
                },
                new BudgetStatus
                {
                    Budget = new Budget { Category = "Entretenimiento", MonthlyAmount = 200 },
                    SpentAmount = 180
                },
                new BudgetStatus
                {
                    Budget = new Budget { Category = "Transporte", MonthlyAmount = 300 },
                    SpentAmount = 250
                }
            };
                }

                _logger.LogInformation("Presupuestos procesados: {Count}", budgetStatus.Count);

                return new ChartData
                {
                    Type = "bar",
                    Labels = budgetStatus.Select(b => b.Budget.Category).ToArray(),
                    Datasets = new List<ChartDataset>
            {
                new ChartDataset
                {
                    Label = "Gastado",
                    Data = budgetStatus.Select(b => b.SpentAmount).ToArray(),
                    BackgroundColor = budgetStatus.Select(b =>
                        b.IsOverBudget ? "#ef4444" :
                        b.UsagePercentage >= 80 ? "#f59e0b" : "#10b981"
                    ).ToArray(),
                    BorderColor = budgetStatus.Select(b =>
                        b.IsOverBudget ? "#dc2626" :
                        b.UsagePercentage >= 80 ? "#d97706" : "#059669"
                    ).ToArray(),
                    BorderWidth = 1
                },
                new ChartDataset
                {
                    Label = "Presupuesto",
                    Data = budgetStatus.Select(b => b.Budget.MonthlyAmount).ToArray(),
                    BackgroundColor = budgetStatus.Select(_ => "rgba(59, 130, 246, 0.1)").ToArray(),
                    BorderColor = budgetStatus.Select(_ => "#3b82f6").ToArray(),
                    BorderWidth = 2,
                    BorderDash = new[] { 5, 5 }
                }
            }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al generar gráfico de progreso de presupuestos para usuario {UserId}", userId);

                return new ChartData
                {
                    Type = "bar",
                    Labels = new[] { "Error" },
                    Datasets = new List<ChartDataset>
            {
                new ChartDataset
                {
                    Label = "Error",
                    Data = new decimal[] { 0 }
                }
            }
                };
            }
        }

        // Métodos adicionales que puedes implementar después
        public async Task<ChartData> GetCategoryComparisonChartAsync(string userId, string chartType = "bar")
        {
            // Para comparar ingresos vs gastos por categoría
            // Lo implementas cuando lo necesites
            return new ChartData();
        }

        public async Task<ChartData> GetSavingsProgressChartAsync(string userId)
        {
            // Para mostrar progreso de ahorros hacia metas
            // Lo implementas cuando lo necesites  
            return new ChartData();
        }

        // Reutilizas tus métodos auxiliares existentes
        private string GetCategoryColor(string category)
        {
            var colors = new Dictionary<string, string>
            {
                {"Alimentación", "#ef4444"},
                {"Transporte", "#f59e0b"},
                {"Vivienda", "#10b981"},
                {"Entretenimiento", "#8b5cf6"},
                {"Salud", "#ec4899"},
                {"Educación", "#06b6d4"},
                {"Ropa", "#84cc16"},
                {"Servicios", "#3b82f6"},
                {"Otros Gastos", "#64748b"}
            };
            
            return colors.ContainsKey(category) ? colors[category] : "#" + Random.Shared.Next(0x1000000).ToString("X6");
        }

        private string GetIncomeCategoryColor(string category)
        {
            var colors = new Dictionary<string, string>
            {
                {"Salario", "#10b981"},
                {"Freelance", "#84cc16"},
                {"Inversiones", "#8b5cf6"},
                {"Bonos", "#f59e0b"},
                {"Comisiones", "#ec4899"},
                {"Alquileres", "#06b6d4"},
                {"Dividendos", "#3b82f6"},
                {"Ventas", "#f97316"},
                {"Otros Ingresos", "#64748b"}
            };

            return colors.ContainsKey(category) ? colors[category] : "#" + Random.Shared.Next(0x1000000).ToString("X6");
        }
        
        
    }
}