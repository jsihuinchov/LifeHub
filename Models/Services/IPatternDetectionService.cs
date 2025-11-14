using LifeHub.Models.Entities;

namespace LifeHub.Models.Services
{
    public interface IPatternDetectionService
    {
        Task<List<HealthPattern>> AnalyzePatternsAsync(string userId);
        Task<List<Correlation>> FindCorrelationsAsync(List<WellnessCheck> data);
        Task<List<string>> GeneratePredictiveInsightsAsync(string userId);
        Task<SymptomCluster> DetectSymptomClustersAsync(List<WellnessCheck> data);
        Task<List<TemporalPattern>> FindTemporalPatternsAsync(List<WellnessCheck> data);
    }

    public class HealthPattern
    {
        public string PatternType { get; set; } = string.Empty; // "SymptomCluster", "Temporal", "Correlation"
        public string Description { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public List<string> RelatedFactors { get; set; } = new();
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    }

    public class Correlation
    {
        public string Factor1 { get; set; } = string.Empty;
        public string Factor2 { get; set; } = string.Empty;
        public double Strength { get; set; }
        public string Direction { get; set; } = string.Empty; // "positive", "negative"
    }

    public class SymptomCluster
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Symptoms { get; set; } = new();
        public int Frequency { get; set; }
        public double Severity { get; set; }
    }

    public class TemporalPattern
    {
        public string Pattern { get; set; } = string.Empty;
        public string TimeFrame { get; set; } = string.Empty; // "morning", "evening", "weekly", "monthly"
        public string Description { get; set; } = string.Empty;
    }
}