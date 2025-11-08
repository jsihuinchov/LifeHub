namespace LifeHub.Models.IA.Results
{
    public class HabitRecommendation
    {
        public string HabitName { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "TimeOptimization", "Consistency", "NewHabit"
        public double Confidence { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class HabitSuccessPrediction
    {
        public double SuccessProbability { get; set; }
        public string ConfidenceLevel { get; set; } = string.Empty; // "High", "Medium", "Low"
        public List<string> KeyFactors { get; set; } = new List<string>();
        public string Recommendation { get; set; } = string.Empty;
    }

    public class HabitPattern
    {
        public string PatternType { get; set; } = string.Empty; // "Weekly", "TimeBased", "CategoryBased"
        public string Description { get; set; } = string.Empty;
        public double Strength { get; set; }
        public string SuggestedAction { get; set; } = string.Empty;
    }

    public class TimeOptimization
    {
        public string BestTimeOfDay { get; set; } = string.Empty; // "Morning", "Afternoon", "Evening"
        public string BestDayOfWeek { get; set; } = string.Empty;
        public double ImprovementPotential { get; set; }
    }

    public class ConsistencyAlert
    {
        public string HabitName { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty; // "Declining", "Inconsistent", "AtRisk"
        public string Message { get; set; } = string.Empty;
        public double CurrentConsistency { get; set; }
        public double TargetConsistency { get; set; }
    }
}