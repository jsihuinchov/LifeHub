using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeHub.Models.Entities
{
    public class Medication
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // 🔥 NUEVAS PROPIEDADES para sistema inteligente
        public int TotalQuantity { get; set; } = 0;
        public int DosagePerIntake { get; set; } = 1;
        public int TimesPerDay { get; set; } = 1;
        public int LowStockAlert { get; set; } = 5;
        public DateTime? LastTaken { get; set; }
        public bool RequiresPrescription { get; set; } = false;
        
        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; }
    }
}