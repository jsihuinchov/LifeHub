// LifeHub/Models/Services/IHealthService.cs
using LifeHub.Models.Entities;
using LifeHub.Models.ViewModels;

namespace LifeHub.Models.Services
{
    public interface IHealthService
    {
        // Medical Appointments
        Task<List<MedicalAppointment>> GetUserAppointmentsAsync(string userId);
        Task<MedicalAppointment?> GetAppointmentByIdAsync(int id, string userId);
        Task<bool> CreateAppointmentAsync(MedicalAppointmentViewModel model, string userId);
        Task<bool> UpdateAppointmentAsync(MedicalAppointmentViewModel model, string userId);
        Task<bool> DeleteAppointmentAsync(int id, string userId);

        // Medications
        Task<List<Medication>> GetUserMedicationsAsync(string userId);
        Task<List<Medication>> GetActiveMedicationsAsync(string userId);
        Task<Medication?> GetMedicationByIdAsync(int id, string userId);
        Task<bool> CreateMedicationAsync(MedicationViewModel model, string userId);
        Task<bool> UpdateMedicationAsync(MedicationViewModel model, string userId);
        Task<bool> DeleteMedicationAsync(int id, string userId);
        Task<bool> ToggleMedicationStatusAsync(int id, string userId);

        // Stats & Dashboard
        Task<HealthStatsViewModel> GetHealthStatsAsync(string userId);
        Task<HealthDashboardViewModel> GetDashboardDataAsync(string userId);

        // Medication API Search - ✅ NUEVOS MÉTODOS
        Task<List<MedicationSuggestion>> SearchMedicationsAsync(string searchTerm);
        Task<MedicationInfo?> GetMedicationDetailsAsync(string medicationId);

        // Reminders
        Task CheckAndSendRemindersAsync();
        Task<List<MedicalAppointment>> GetAppointmentsNeedingReminderAsync();

        // Wellness Check Methods
        Task<bool> SaveWellnessCheckAsync(WellnessCheckViewModel model, string userId);
        Task<WellnessCheck?> GetTodayWellnessCheckAsync(string userId);
        Task<List<WellnessCheck>> GetWellnessHistoryAsync(string userId, int days = 7);
        Task<WellnessDashboardViewModel> GetWellnessDashboardAsync(string userId);

        // Alertas inteligentes
        Task<List<Medication>> GetMedicationsNeedingRefillAsync(string userId);
        Task<List<Medication>> GetTodayMedicationsAsync(string userId);
        Task<List<Medication>> GetMedicationsByUserAsync(string userEmail);
        Task<WeeklyHealthStats> GetWeeklyHealthStatsAsync(string userEmail);
        Task<List<MedicalAppointment>> GetUpcomingAppointmentsAsync(string userEmail, int daysAhead = 7);
        
        Task<List<HealthPattern>> DetectHealthPatternsAsync(string userId);
        Task<AppointmentPreparation> GenerateAppointmentPreparationAsync(int appointmentId);
        Task<MedicalReport> GenerateMedicalReportAsync(string userId, DateTime startDate, DateTime endDate);
        Task<List<string>> GetPredictiveInsightsAsync(string userId);
    }
    
    public class WeeklyHealthStats
    {
        public double AverageMood { get; set; }
        public double AverageEnergy { get; set; }
        public int MedicationsTaken { get; set; }
        public int TotalMedications { get; set; }
        public int AppointmentsThisWeek { get; set; }
        public string Recommendations { get; set; } = string.Empty;
    }
}