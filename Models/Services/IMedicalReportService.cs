using LifeHub.Models.Entities;

namespace LifeHub.Models.Services
{
    public interface IMedicalReportService
    {
        Task<MedicalReport> GenerateReportAsync(string userId, DateRange range);
        Task<string> GenerateExecutiveSummaryAsync(List<WellnessCheck> data);
        Task<List<HealthTrend>> IdentifyTrendsAsync(List<WellnessCheck> data);
        Task<List<HealthRecommendation>> GenerateRecommendationsAsync(string userId);
    }

    public class MedicalReport
    {
        public string ExecutiveSummary { get; set; } = string.Empty;
        public List<HealthTrend> Trends { get; set; } = new();
        public List<HealthPattern> Patterns { get; set; } = new();
        public List<HealthRecommendation> Recommendations { get; set; } = new();
        public ReportStatistics Statistics { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class HealthTrend
    {
        public string Metric { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty; // "improving", "declining", "stable"
        public double ChangePercentage { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class HealthRecommendation
    {
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty; // "high", "medium", "low"
        public string ActionSteps { get; set; } = string.Empty;
    }

    public class ReportStatistics
    {
        public double AverageEnergy { get; set; }
        public double AverageSleep { get; set; }
        public int TotalEntries { get; set; }
        public int SymptomDays { get; set; }
        public double MedicationAdherence { get; set; }
    }

    public class DateRange
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}