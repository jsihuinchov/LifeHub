namespace LifeHub.Models.IA.Results
{
    public class BudgetRecommendation
    {
        public string Category { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public decimal CurrentSpending { get; set; }
        public decimal RecommendedBudget { get; set; }
        public decimal PotentialSavings { get; set; }
        public double Confidence { get; set; }
        public string Reasoning { get; set; } = string.Empty;
    }
}