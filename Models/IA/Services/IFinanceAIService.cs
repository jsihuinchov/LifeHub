using LifeHub.Models.IA.Results;

namespace LifeHub.Models.IA.Services
{
    public interface IFinanceAIService
    {
        Task<List<FinancialAlert>> DetectSpendingAnomaliesAsync(string userId);
        Task<decimal> PredictNextMonthSpendingAsync(string userId);
        Task<List<BudgetRecommendation>> GenerateBudgetRecommendationsAsync(string userId);
        Task<SpendingPatterns> AnalyzeSpendingPatternsAsync(string userId);
        Task<string> PredictTransactionCategoryAsync(string description, decimal amount);
    }
}