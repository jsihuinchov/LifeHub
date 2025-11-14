using System.ComponentModel.DataAnnotations;
using LifeHub.Models.Entities;

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

        public string? HealthConcerns { get; set; }
        public string? QuestionsForDoctor { get; set; }
        public string? PostAppointmentSummary { get; set; }
        public string? NextSteps { get; set; }
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
        
        public int TotalQuantity { get; set; }
        public int DosagePerIntake { get; set; } = 1;
        public int TimesPerDay { get; set; } = 1;
        public int LowStockAlert { get; set; } = 5;
        public bool RequiresPrescription { get; set; }
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

    // ✅ NUEVO WELLNESS VIEWMODEL SIN DUPLICADOS
    public class WellnessCheckViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Selecciona cómo te sientes hoy")]
        [Display(Name = "Estado General")]
        public WellnessLevel GeneralWellness { get; set; } = WellnessLevel.Regular;

        [Display(Name = "Síntomas")]
        public List<HealthSymptom> SelectedSymptoms { get; set; } = new();

        [Display(Name = "Otro síntoma")]
        [MaxLength(100)]
        public string? CustomSymptom { get; set; }

        [Required(ErrorMessage = "Selecciona tu nivel de energía")]
        [Range(1, 10, ErrorMessage = "El nivel de energía debe ser entre 1 y 10")]
        [Display(Name = "Nivel de Energía")]
        public int EnergyLevel { get; set; } = 5;

        [Required(ErrorMessage = "Selecciona la calidad de tu sueño")]
        [Range(1, 10, ErrorMessage = "La calidad de sueño debe ser entre 1 y 10")]
        [Display(Name = "Calidad de Sueño")]
        public int SleepQuality { get; set; } = 5;

        [Display(Name = "Nota del día")]
        [MaxLength(500, ErrorMessage = "La nota no puede tener más de 500 caracteres")]
        public string? QuickNote { get; set; }

        public bool TookMedications { get; set; }
        public string? MedicationNotes { get; set; }

        // Propiedades para la UI
        public bool HasEntryToday { get; set; }
        public WellnessCheck? TodayEntry { get; set; }

        internal static object FromEntity(WellnessCheck todayCheck)
        {
            throw new NotImplementedException();
        }
    }
    
    public class WellnessDashboardViewModel
    {
        public WellnessCheckViewModel? TodayCheck { get; set; }
        public List<WellnessCheck> Last7Days { get; set; } = new();
        public List<Medication> ActiveMedications { get; set; } = new();
        public List<MedicalAppointment> UpcomingAppointments { get; set; } = new();
        public List<Medication> LowStockMedications { get; set; } = new();
        public int CurrentStreak { get; set; }
        public double AverageEnergy { get; set; }
        public string MostCommonMood { get; set; } = "neutral";

        public List<string> HealthInsights { get; set; } = new();
        public Dictionary<string, int> SymptomFrequency { get; set; } = new();
        public double AverageSleepQuality { get; set; }
        public WellnessLevel MostCommonWellness { get; set; }
    }
}