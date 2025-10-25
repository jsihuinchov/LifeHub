using System.Collections.Generic;

namespace LifeHub.Models.ViewModels
{
    public class HabitDetailStatsViewModel
    {
        // Información del hábito
        public int HabitId { get; set; }
        public string HabitName { get; set; } = string.Empty;
        public string HabitDescription { get; set; } = string.Empty;
        public string HabitIcon { get; set; } = string.Empty;
        public string HabitColor { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Frequency { get; set; }
        public int? TargetCount { get; set; }
        public DateTime StartDate { get; set; }

        // Estadísticas básicas
        public int TotalCompletions { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        
        // Completados por período
        public bool TodayCompleted { get; set; }
        public int WeekCompletions { get; set; }
        public int MonthCompletions { get; set; }
        public int YearCompletions { get; set; }
        
        // Métricas avanzadas
        public double AverageCompletionsPerWeek { get; set; }
        public double SuccessRate { get; set; }
        public string BestDayOfWeek { get; set; } = string.Empty;
        public string MostProductiveMonth { get; set; } = string.Empty;

        // Datos para gráficos
        public Dictionary<string, int> WeeklyData { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> MonthlyData { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> DailyCompletionPattern { get; set; } = new Dictionary<string, int>();

        // Insights y recomendaciones
        public List<string> Insights { get; set; } = new List<string>();

        // Propiedades calculadas para la vista
        public string SuccessRateFormatted => $"{SuccessRate:0.0}%";
        public string AverageCompletionsFormatted => $"{AverageCompletionsPerWeek:0.0}";
        public string StreakEmoji => CurrentStreak switch
        {
            >= 30 => "🔥",
            >= 14 => "⚡", 
            >= 7 => "🌟",
            >= 3 => "⭐",
            _ => "🌱"
        };
        public string ConsistencyLevel => SuccessRate switch
        {
            >= 90 => "Excelente",
            >= 75 => "Muy buena", 
            >= 60 => "Buena",
            >= 40 => "En desarrollo",
            _ => "Principiante"
        };
    }
}