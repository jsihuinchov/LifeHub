using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeHub.Models.Entities
{
    public class MedicalAppointment
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty; // Cambiado de int a string
        public string Title { get; set; } = string.Empty;
        public string? DoctorName { get; set; }
        public string? Specialty { get; set; }
        public DateTime AppointmentDate { get; set; }
        public int Duration { get; set; } = 30;
        public string? Location { get; set; }
        public string? Notes { get; set; }
        public bool ReminderSent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; } // Añadido
    }
}