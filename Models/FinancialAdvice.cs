// Models/FinancialAdvice.cs
namespace LifeHub.Models.Entities
{
    public class FinancialAdvice
    {
        public int Id { get; set; }
        public string Advice { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty; // Beginner, Intermediate, Advanced
    }
}