using System.ComponentModel.DataAnnotations;
using LifeHub.Models.Entities; // ✅ AÑADIR ESTA REFERENCIA

namespace LifeHub.Models.ViewModels
{
    public class MedicalAppointmentViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "El título es requerido")]
        [StringLength(100, ErrorMessage = "El título no puede exceder 100 caracteres")]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(100, ErrorMessage = "El nombre del doctor no puede exceder 100 caracteres")]
        public string? DoctorName { get; set; }
        
        [StringLength(50, ErrorMessage = "La especialidad no puede exceder 50 caracteres")]
        public string? Specialty { get; set; }
        
        [Required(ErrorMessage = "La fecha de la cita es requerida")]
        public DateTime AppointmentDate { get; set; } = DateTime.Now.AddDays(1);
        
        [Range(1, 480, ErrorMessage = "La duración debe estar entre 1 y 480 minutos")]
        public int Duration { get; set; } = 30;
        
        [StringLength(200, ErrorMessage = "La ubicación no puede exceder 200 caracteres")]
        public string? Location { get; set; }
        
        [StringLength(500, ErrorMessage = "Las notas no pueden exceder 500 caracteres")]
        public string? Notes { get; set; }
        
        public bool ReminderSent { get; set; }
    }

    public class MedicationViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "El nombre del medicamento es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(50, ErrorMessage = "La dosis no puede exceder 50 caracteres")]
        public string? Dosage { get; set; }
        
        [StringLength(100, ErrorMessage = "La frecuencia no puede exceder 100 caracteres")]
        public string? Frequency { get; set; }
        
        public DateTime? StartDate { get; set; } = DateTime.Today;
        public DateTime? EndDate { get; set; }
        
        [StringLength(500, ErrorMessage = "Las notas no pueden exceder 500 caracteres")]
        public string? Notes { get; set; }
        
        public bool IsActive { get; set; } = true;
    }

    public class HealthDashboardViewModel
    {
        public List<MedicalAppointment> UpcomingAppointments { get; set; } = new();
        public List<Medication> ActiveMedications { get; set; } = new();
        public HealthStatsViewModel Stats { get; set; } = new();
        public int AppointmentCount { get; set; }
        public int MedicationCount { get; set; }
    }

    public class HealthStatsViewModel
    {
        public int TotalAppointments { get; set; }
        public int UpcomingAppointments { get; set; }
        public int ActiveMedications { get; set; }
        public int CompletedAppointments { get; set; }
        public double AverageAppointmentsPerMonth { get; set; }
        public string MostFrequentSpecialty { get; set; } = string.Empty;
    }
}