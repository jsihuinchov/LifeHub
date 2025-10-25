// LifeHub/Models/Services/IHealthService.cs
using LifeHub.Models.Entities;
using LifeHub.Models.ViewModels;

namespace LifeHub.Models.Services
{
    public interface IHealthService
    {
        // Medical Appointments
        Task<List<MedicalAppointment>> GetUserAppointmentsAsync(string userId);
        Task<List<MedicalAppointment>> GetUpcomingAppointmentsAsync(string userId, int days = 30);
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
    }
}