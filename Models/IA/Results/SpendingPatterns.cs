namespace LifeHub.Models.IA.Results
{
    public class SpendingPatterns
    {
        public string UserId { get; set; } = string.Empty;
        public List<CategoryPattern> CategoryPatterns { get; set; } = new();
        public List<WeeklyPattern> WeeklyPatterns { get; set; } = new();
        public List<MonthlyTrend> MonthlyTrends { get; set; } = new();
        public string MostSpentCategory { get; set; } = string.Empty;
        public string MostFrequentCategory { get; set; } = string.Empty;
        public decimal AverageMonthlySpending { get; set; }
        public decimal ProjectedYearlySpending { get; set; }
    }

    public class CategoryPattern
    {
        public string Category { get; set; } = string.Empty;
        public decimal AverageMonthlyAmount { get; set; }
        public decimal PercentageOfTotal { get; set; }
        public decimal GrowthRate { get; set; }
        public string Trend { get; set; } = string.Empty;
    }

    public class WeeklyPattern
    {
        public string DayOfWeek { get; set; } = string.Empty;
        public decimal AverageSpending { get; set; }
        public int TransactionCount { get; set; }
    }

    public class MonthlyTrend
    {
        public string Month { get; set; } = string.Empty;
        public decimal TotalSpending { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal Savings { get; set; }
        public decimal SavingsRate { get; set; }
    }
}