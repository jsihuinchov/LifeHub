using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeHub.Models.Entities
{
    public class Medication
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty; // Cambiado de int a string
        public string Name { get; set; } = string.Empty;
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; } // Añadido
    }
}