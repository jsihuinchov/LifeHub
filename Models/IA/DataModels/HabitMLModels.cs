using Microsoft.ML.Data;

namespace LifeHub.Models.IA.DataModels
{
    // Modelo para características de hábitos
    public class HabitFeatures
    {
        public float Frequency { get; set; }
        public float UserConsistency { get; set; }
        public float Category { get; set; }
        public float TimeOfDayPreference { get; set; }
        public float PreviousHabitsSuccessRate { get; set; }
        public float HabitComplexity { get; set; }
        public bool Success { get; set; }
    }

    public class SuccessPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool PredictedLabel { get; set; }
        
        [ColumnName("Probability")]
        public float Probability { get; set; }
        
        [ColumnName("Score")]
        public float Score { get; set; }
    }

    // Modelos auxiliares
    public class UserFeatures
    {
        public float ConsistencyScore { get; set; }
        public float SuccessRate { get; set; }
        public float BestTimeOfDay { get; set; }
        public int TotalHabits { get; set; }
    }

    public class HabitCluster
    {
        public int ClusterId { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<int> HabitIds { get; set; } = new List<int>();
    }

    public class CompletionFeatures
    {
        public float DayOfWeek { get; set; }
        public float TimeOfDay { get; set; }
        public float HabitDuration { get; set; }
        public bool Success { get; set; }
    }

    public class TrendPrediction
    {
        public DateTime Date { get; set; }
        public float PredictedCompletions { get; set; }
        public bool IsAnomaly { get; set; }
    }

    public class TimeSeriesPoint
    {
        public DateTime Date { get; set; }
        public int Completions { get; set; }
        public float SuccessRate { get; set; }
    }
}