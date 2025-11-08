using Microsoft.ML.Data;

namespace LifeHub.Models.IA.DataModels
{
    public class HabitData
    {
        [LoadColumn(0)]
        public float Frequency { get; set; }
        
        [LoadColumn(1)]
        public float UserHistoricalSuccessRate { get; set; }
        
        [LoadColumn(2)]
        public float CategoryDifficulty { get; set; }
        
        [LoadColumn(3)]
        public float TimeOfDayPreference { get; set; }
        
        [LoadColumn(4)]
        public float PreviousHabitsCount { get; set; }
        
        [LoadColumn(5)]
        public bool Success { get; set; } // Esto es lo que queremos predecir
    }

    public class HabitPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool Success { get; set; }
        
        [ColumnName("Probability")]
        public float Probability { get; set; }
        
        [ColumnName("Score")]
        public float Score { get; set; }
    }
}