namespace LifeHub.Models.ViewModels
{
    public class HabitStatsViewModel
    {
        public int TotalHabits { get; set; }
        public int ActiveHabits { get; set; }
        public int TotalCompletions { get; set; }
        public int TodayCompletions { get; set; }
        public int WeekCompletions { get; set; }
        public int MonthCompletions { get; set; }
        public int LongestStreak { get; set; }
        public int CurrentStreak { get; set; }
        public double CompletionRate { get; set; }
        public Dictionary<string, int> TopCategories { get; set; } = new Dictionary<string, int>();
        
        // NUEVAS MÉTRICAS PROPUESTAS:
        public double WeeklyConsistency { get; set; }
        public string MostSuccessfulHabit { get; set; } = string.Empty;
        public string MostDifficultHabit { get; set; } = string.Empty;
        public double ImprovementRate { get; set; }
        public string BestTimeOfDay { get; set; } = string.Empty;
        public Dictionary<string, int> WeeklyDistribution { get; set; } = new Dictionary<string, int>();
        public int PerfectWeeks { get; set; }
        public Dictionary<string, int> WeeklyTrendData { get; set; } = new Dictionary<string, int>();


        // Métodos de conveniencia
        public string CompletionRateFormatted => $"{CompletionRate:0.0}%";
        public string StreakEmoji => CurrentStreak switch
        {
            >= 30 => "🔥",
            >= 14 => "⚡",
            >= 7 => "🌟",
            >= 3 => "⭐",
            _ => "🌱"
        };
    }
}