namespace LifeHub.Models.IA.Results
{
    public class FinancialAlert
    {
        public string AlertType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal ExpectedAmount { get; set; }
        public decimal DeviationPercentage { get; set; }
        public DateTime DetectedDate { get; set; } = DateTime.UtcNow;
        public string Category { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
    }

    public enum AlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}