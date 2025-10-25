using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeHub.Models.Entities
{
    public class Habit
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        public string? Category { get; set; }
        
        [Required]
        public int Frequency { get; set; }
        
        public int? TargetCount { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string ColorCode { get; set; } = "#3B82F6";
        public string Icon { get; set; } = "📝";
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // ✅ NUEVO: Campo para favoritos
        public bool IsFavorite { get; set; } = false;
        
        // ✅ NUEVO: Orden para favoritos
        public int FavoriteOrder { get; set; } = 0;
        
        public List<HabitCompletion>? HabitCompletions { get; set; }
        
        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; }
    }
}