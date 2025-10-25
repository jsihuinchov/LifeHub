using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeHub.Models
{
    public class SubscriptionPlan
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string LongDescription { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationDays { get; set; } = 30;
        public bool IsActive { get; set; } = true;
        
        // Límites
        public int MaxHabits { get; set; } = 3;
        public int MaxTransactions { get; set; } = 50;
        public int MaxBudgets { get; set; } = 5; // ✅ NUEVA PROPIEDAD
        
        // Características
        public bool HasCommunityAccess { get; set; }
        public bool HasAdvancedAnalytics { get; set; }
        public bool HasAIFeatures { get; set; }
        public int StorageMB { get; set; } = 10;
        public string ColorCode { get; set; } = "#3B82F6";
        public bool IsFeatured { get; set; }
        public int SortOrder { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public JsonDocument? Features { get; set; }
        
        // Propiedades calculadas
        public string FormattedPrice => Price == 0 ? "Gratis" : $"${Price:0.00}";
        public string Period => DurationDays == 30 ? "mes" : "año";
        public bool IsPopular => Name.Contains("Premium") || Name.Contains("Máxima");
        
        public bool HasFeature(string feature)
        {
            return feature switch
            {
                "community" => HasCommunityAccess,
                "analytics" => HasAdvancedAnalytics,
                "ai" => HasAIFeatures,
                _ => false
            };
        }
    }
}