using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeHub.Models.Entities
{
    public class HabitCompletion
    {
        public int Id { get; set; }
        public int HabitId { get; set; }
        
        [Required]
        public DateTime CompletionDate { get; set; }
        
        public bool Completed { get; set; }
        public string? Notes { get; set; }
        public int StreakCount { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // ✅ Siempre UTC
        
        public Habit? Habit { get; set; }
    }
}