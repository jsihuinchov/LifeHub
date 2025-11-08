using Microsoft.ML;
using LifeHub.Models.Entities;
using LifeHub.Models.Services;
using LifeHub.Models.IA.Results;
using LifeHub.Services;

namespace LifeHub.Models.IA.Services
{
    public class FinanceAIService : IFinanceAIService
    {
        private readonly MLContext _mlContext;
        private readonly IFinanceService _financeService;
        private readonly ILogger<FinanceAIService> _logger;

        public FinanceAIService(MLContext mlContext, IFinanceService financeService, ILogger<FinanceAIService> logger)
        {
            _mlContext = mlContext;
            _financeService = financeService;
            _logger = logger;
        }

        public async Task<List<FinancialAlert>> DetectSpendingAnomaliesAsync(string userId)
        {
            try
            {
                _logger.LogInformation("🔍 Iniciando detección de anomalías para usuario: {UserId}", userId);
                
                var transactions = await _financeService.GetUserTransactionsAsync(userId);
                var alerts = new List<FinancialAlert>();

                if (!transactions.Any())
                {
                    _logger.LogWarning("No hay transacciones para analizar");
                    return alerts;
                }

                // Simular detección de anomalías (por ahora datos de ejemplo)
                var expenses = transactions.Where(t => t.TransactionType == 1).ToList();
                if (expenses.Any())
                {
                    var averageExpense = expenses.Average(t => t.Amount);
                    var maxExpense = expenses.Max(t => t.Amount);
                    
                    if (maxExpense > averageExpense * 3)
                    {
                        alerts.Add(new FinancialAlert
                        {
                            AlertType = "UnusualSpending",
                            Message = $"Gasto inusual detectado: {maxExpense:C}",
                            Amount = maxExpense,
                            ExpectedAmount = averageExpense,
                            DeviationPercentage = ((maxExpense - averageExpense) / averageExpense) * 100,
                            Category = "Detección Automática",
                            Severity = AlertSeverity.Medium
                        });
                    }
                }

                // Verificar presupuestos
                var budgetStatus = await _financeService.GetBudgetStatusAsync(userId);
                foreach (var budget in budgetStatus)
                {
                    if (budget.UsagePercentage >= 90)
                    {
                        alerts.Add(new FinancialAlert
                        {
                            AlertType = "BudgetExceeded",
                            Message = $"Presupuesto de {budget.Budget.Category} al {budget.UsagePercentage:0}%",
                            Amount = budget.SpentAmount,
                            ExpectedAmount = budget.Budget.MonthlyAmount,
                            DeviationPercentage = budget.UsagePercentage - 100,
                            Category = budget.Budget.Category,
                            Severity = budget.UsagePercentage >= 100 ? AlertSeverity.High : AlertSeverity.Medium
                        });
                    }
                }

                _logger.LogInformation("✅ Detección completada. Alertas encontradas: {AlertCount}", alerts.Count);
                return alerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en detección de anomalías para usuario: {UserId}", userId);
                return new List<FinancialAlert>();
            }
        }

        public async Task<decimal> PredictNextMonthSpendingAsync(string userId)
        {
            try
            {
                var transactions = await _financeService.GetUserTransactionsAsync(userId);
                var expenses = transactions.Where(t => t.TransactionType == 1).ToList();

                if (!expenses.Any())
                    return 0;

                // Predicción simple basada en promedio móvil
                var last3Months = expenses
                    .Where(t => t.TransactionDate >= DateTime.UtcNow.AddMonths(-3))
                    .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
                    .Select(g => new { Month = g.Key, Total = g.Sum(t => t.Amount) })
                    .OrderByDescending(x => x.Month)
                    .Take(3)
                    .ToList();

                if (last3Months.Count < 2)
                    return expenses.Average(t => t.Amount) * 4;

                var weightedAverage = (last3Months[0].Total * 0.5m) + 
                                    (last3Months[1].Total * 0.3m) + 
                                    (last3Months.Count > 2 ? last3Months[2].Total * 0.2m : 0);

                _logger.LogInformation("📊 Predicción de gastos: {Prediction:C} para usuario: {UserId}", weightedAverage, userId);
                return Math.Round(weightedAverage, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en predicción de gastos para usuario: {UserId}", userId);
                return 0;
            }
        }

        public async Task<List<BudgetRecommendation>> GenerateBudgetRecommendationsAsync(string userId)
        {
            var recommendations = new List<BudgetRecommendation>();
            
            try
            {
                var transactions = await _financeService.GetUserTransactionsAsync(userId);
                var expenses = transactions.Where(t => t.TransactionType == 1).ToList();
                var budgets = await _financeService.GetUserBudgetsAsync(userId);

                if (!expenses.Any())
                    return recommendations;

                // Analizar categorías con mayor gasto
                var categoryAnalysis = expenses
                    .Where(t => !string.IsNullOrEmpty(t.Category))
                    .GroupBy(t => t.Category!)
                    .Select(g => new
                    {
                        Category = g.Key,
                        TotalSpent = g.Sum(t => t.Amount),
                        AverageSpent = g.Average(t => t.Amount),
                        Count = g.Count()
                    })
                    .Where(x => x.TotalSpent > 50)
                    .OrderByDescending(x => x.TotalSpent)
                    .Take(3)
                    .ToList();

                foreach (var category in categoryAnalysis)
                {
                    var currentBudget = budgets.FirstOrDefault(b => b.Category == category.Category);
                    var recommendedBudget = (decimal)category.AverageSpent * 1.1m; // +10% para flexibilidad

                    recommendations.Add(new BudgetRecommendation
                    {
                        Category = category.Category,
                        Recommendation = $"Considera aumentar el presupuesto a {recommendedBudget:C} para {category.Category}",
                        CurrentSpending = (decimal)category.TotalSpent,
                        RecommendedBudget = recommendedBudget,
                        PotentialSavings = 0,
                        Confidence = 0.8,
                        Reasoning = $"Basado en tu gasto promedio de {category.AverageSpent:C} por transacción"
                    });
                }

                _logger.LogInformation("✅ Recomendaciones de presupuesto generadas: {Count}", recommendations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generando recomendaciones de presupuesto");
            }

            return recommendations;
        }

        public async Task<SpendingPatterns> AnalyzeSpendingPatternsAsync(string userId)
        {
            var patterns = new SpendingPatterns { UserId = userId };
            
            try
            {
                var transactions = await _financeService.GetUserTransactionsAsync(userId);
                var expenses = transactions.Where(t => t.TransactionType == 1).ToList();

                if (!expenses.Any())
                    return patterns;

                // Analizar patrones por categoría
                var categoryData = expenses
                    .Where(t => !string.IsNullOrEmpty(t.Category))
                    .GroupBy(t => t.Category!)
                    .Select(g => new CategoryPattern
                    {
                        Category = g.Key,
                        AverageMonthlyAmount = g.Average(t => t.Amount),
                        PercentageOfTotal = g.Sum(t => t.Amount) / expenses.Sum(t => t.Amount) * 100,
                        GrowthRate = 0, // Por simplificar por ahora
                        Trend = "Stable"
                    })
                    .OrderByDescending(c => c.AverageMonthlyAmount)
                    .Take(5)
                    .ToList();

                patterns.CategoryPatterns = categoryData;
                
                // Analizar patrones semanales
                var daysOfWeek = new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
                var weeklyData = expenses
                    .GroupBy(t => (int)t.TransactionDate.DayOfWeek)
                    .Select(g => new WeeklyPattern
                    {
                        DayOfWeek = daysOfWeek[g.Key],
                        AverageSpending = g.Average(t => t.Amount),
                        TransactionCount = g.Count()
                    })
                    .OrderBy(w => Array.IndexOf(daysOfWeek, w.DayOfWeek))
                    .ToList();

                patterns.WeeklyPatterns = weeklyData;

                patterns.MostSpentCategory = categoryData.FirstOrDefault()?.Category ?? "N/A";
                patterns.AverageMonthlySpending = expenses.Sum(t => t.Amount);
                patterns.ProjectedYearlySpending = patterns.AverageMonthlySpending * 12;

                _logger.LogInformation("✅ Análisis de patrones completado para usuario: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error analizando patrones de gasto");
            }

            return patterns;
        }

        public async Task<string> PredictTransactionCategoryAsync(string description, decimal amount)
        {
            // Lógica simple basada en palabras clave
            var descriptionLower = description.ToLowerInvariant();

            var categoryKeywords = new Dictionary<string, string[]>
            {
                ["Alimentación"] = new[] { "supermercado", "comida", "restaurante", "cena", "almuerzo", "desayuno" },
                ["Transporte"] = new[] { "gasolina", "estacionamiento", "uber", "taxi", "transporte" },
                ["Entretenimiento"] = new[] { "cine", "netflix", "spotify", "playstation", "juego" },
                ["Servicios"] = new[] { "luz", "agua", "gas", "internet", "teléfono" },
                ["Salud"] = new[] { "farmacia", "médico", "hospital", "seguro" }
            };

            foreach (var category in categoryKeywords)
            {
                if (category.Value.Any(keyword => descriptionLower.Contains(keyword)))
                    return category.Key;
            }

            return amount > 1000 ? "Grandes Gastos" : "Otros Gastos";
        }
    }
}