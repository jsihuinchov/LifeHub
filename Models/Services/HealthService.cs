using Microsoft.EntityFrameworkCore;
using LifeHub.Data;
using LifeHub.Models.Entities;
using LifeHub.Models.ViewModels;
using Microsoft.Extensions.Logging;

namespace LifeHub.Models.Services
{
    public class HealthService : IHealthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HealthService> _logger;
        private readonly IMedicationApiService _medicationApiService;

        public HealthService(
            ApplicationDbContext context,
            ILogger<HealthService> logger,
            IMedicationApiService medicationApiService) // ✅ Asegurar que este parámetro esté
        {
            _context = context;
            _logger = logger;
            _medicationApiService = medicationApiService;
            _logger.LogInformation("✅ HealthService inicializado con MedicationApiService");
        }

        public async Task<List<MedicationSuggestion>> SearchMedicationsAsync(string searchTerm)
        {
            _logger.LogInformation("🔍 [HEALTH SERVICE] Buscando: {SearchTerm}", searchTerm);

            if (_medicationApiService == null)
            {
                _logger.LogError("❌ [HEALTH SERVICE] MedicationApiService es NULL!");
                return new List<MedicationSuggestion>();
            }

            try
            {
                var results = await _medicationApiService.SearchMedicationsAsync(searchTerm);
                _logger.LogInformation("✅ [HEALTH SERVICE] Obtenidos {Count} resultados", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [HEALTH SERVICE] Error en SearchMedicationsAsync");
                return new List<MedicationSuggestion>();
            }
        }

        public async Task<MedicationInfo?> GetMedicationDetailsAsync(string medicationId)
        {
            return await _medicationApiService.GetMedicationDetailsAsync(medicationId);
        }

        // Medical Appointments
        public async Task<List<MedicalAppointment>> GetUserAppointmentsAsync(string userId)
        {
            return await _context.MedicalAppointments
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
        }

        public async Task<List<MedicalAppointment>> GetUpcomingAppointmentsAsync(string userId, int days = 30)
        {
            // ✅ CORREGIDO: Usar UTC para PostgreSQL
            var startDate = DateTime.UtcNow.Date;
            var endDate = startDate.AddDays(days);
            
            return await _context.MedicalAppointments
                .Where(a => a.UserId == userId && 
                           a.AppointmentDate >= startDate && 
                           a.AppointmentDate <= endDate)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();
        }

        public async Task<MedicalAppointment?> GetAppointmentByIdAsync(int id, string userId)
        {
            return await _context.MedicalAppointments
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        }

        public async Task<bool> CreateAppointmentAsync(MedicalAppointmentViewModel model, string userId)
        {
            try
            {
                // ✅ CORREGIDO: Convertir a UTC para PostgreSQL
                var appointmentDate = model.AppointmentDate;
                if (appointmentDate.Kind == DateTimeKind.Local)
                {
                    appointmentDate = appointmentDate.ToUniversalTime();
                }

                var appointment = new MedicalAppointment
                {
                    UserId = userId,
                    Title = model.Title,
                    DoctorName = model.DoctorName,
                    Specialty = model.Specialty,
                    AppointmentDate = appointmentDate,
                    Duration = model.Duration,
                    Location = model.Location,
                    Notes = model.Notes,
                    ReminderSent = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.MedicalAppointments.Add(appointment);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Cita médica creada: {AppointmentId} para usuario {UserId}", appointment.Id, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear cita médica para usuario {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UpdateAppointmentAsync(MedicalAppointmentViewModel model, string userId)
        {
            try
            {
                var appointment = await GetAppointmentByIdAsync(model.Id, userId);
                if (appointment == null) return false;

                // ✅ CORREGIDO: Convertir a UTC para PostgreSQL
                var appointmentDate = model.AppointmentDate;
                if (appointmentDate.Kind == DateTimeKind.Local)
                {
                    appointmentDate = appointmentDate.ToUniversalTime();
                }

                appointment.Title = model.Title;
                appointment.DoctorName = model.DoctorName;
                appointment.Specialty = model.Specialty;
                appointment.AppointmentDate = appointmentDate;
                appointment.Duration = model.Duration;
                appointment.Location = model.Location;
                appointment.Notes = model.Notes;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Cita médica actualizada: {AppointmentId} para usuario {UserId}", appointment.Id, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar cita médica {AppointmentId} para usuario {UserId}", model.Id, userId);
                return false;
            }
        }

        public async Task<bool> DeleteAppointmentAsync(int id, string userId)
        {
            try
            {
                var appointment = await GetAppointmentByIdAsync(id, userId);
                if (appointment == null) return false;

                _context.MedicalAppointments.Remove(appointment);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Cita médica eliminada: {AppointmentId} para usuario {UserId}", id, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar cita médica {AppointmentId} para usuario {UserId}", id, userId);
                return false;
            }
        }

        // Medications
        public async Task<List<Medication>> GetUserMedicationsAsync(string userId)
        {
            return await _context.Medications
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Medication>> GetActiveMedicationsAsync(string userId)
        {
            return await _context.Medications
                .Where(m => m.UserId == userId && m.IsActive)
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<Medication?> GetMedicationByIdAsync(int id, string userId)
        {
            return await _context.Medications
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
        }

        public async Task<bool> CreateMedicationAsync(MedicationViewModel model, string userId)
        {
            try
            {
                // ✅ CORREGIDO: Convertir fechas a UTC si es necesario
                var startDate = model.StartDate?.ToUniversalTime();
                var endDate = model.EndDate?.ToUniversalTime();

                var medication = new Medication
                {
                    UserId = userId,
                    Name = model.Name,
                    Dosage = model.Dosage,
                    Frequency = model.Frequency,
                    StartDate = startDate,
                    EndDate = endDate,
                    Notes = model.Notes,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Medications.Add(medication);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Medicamento creado: {MedicationId} para usuario {UserId}", medication.Id, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear medicamento para usuario {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UpdateMedicationAsync(MedicationViewModel model, string userId)
        {
            try
            {
                var medication = await GetMedicationByIdAsync(model.Id, userId);
                if (medication == null) return false;

                // ✅ CORREGIDO: Convertir fechas a UTC si es necesario
                var startDate = model.StartDate?.ToUniversalTime();
                var endDate = model.EndDate?.ToUniversalTime();

                medication.Name = model.Name;
                medication.Dosage = model.Dosage;
                medication.Frequency = model.Frequency;
                medication.StartDate = startDate;
                medication.EndDate = endDate;
                medication.Notes = model.Notes;
                medication.IsActive = model.IsActive;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Medicamento actualizado: {MedicationId} para usuario {UserId}", medication.Id, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar medicamento {MedicationId} para usuario {UserId}", model.Id, userId);
                return false;
            }
        }

        public async Task<bool> DeleteMedicationAsync(int id, string userId)
        {
            try
            {
                var medication = await GetMedicationByIdAsync(id, userId);
                if (medication == null) return false;

                _context.Medications.Remove(medication);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Medicamento eliminado: {MedicationId} para usuario {UserId}", id, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar medicamento {MedicationId} para usuario {UserId}", id, userId);
                return false;
            }
        }

        public async Task<bool> ToggleMedicationStatusAsync(int id, string userId)
        {
            try
            {
                var medication = await GetMedicationByIdAsync(id, userId);
                if (medication == null) return false;

                medication.IsActive = !medication.IsActive;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Estado de medicamento cambiado: {MedicationId} - Activo: {IsActive}", id, medication.IsActive);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado de medicamento {MedicationId}", id);
                return false;
            }
        }

        // Stats & Dashboard
        public async Task<HealthStatsViewModel> GetHealthStatsAsync(string userId)
        {
            var appointments = await GetUserAppointmentsAsync(userId);
            var medications = await GetUserMedicationsAsync(userId);
            
            // ✅ CORREGIDO: Usar UTC para comparaciones
            var now = DateTime.UtcNow;
            var oneMonthAgo = now.AddMonths(-1);

            var stats = new HealthStatsViewModel
            {
                TotalAppointments = appointments.Count,
                UpcomingAppointments = appointments.Count(a => a.AppointmentDate >= now),
                ActiveMedications = medications.Count(m => m.IsActive),
                CompletedAppointments = appointments.Count(a => a.AppointmentDate < now)
            };

            // Calcular promedio mensual de citas
            var recentAppointments = appointments.Count(a => a.AppointmentDate >= oneMonthAgo);
            stats.AverageAppointmentsPerMonth = Math.Round(recentAppointments / 1.0, 1);

            // Especialidad más frecuente
            var specialtyGroups = appointments
                .Where(a => !string.IsNullOrEmpty(a.Specialty))
                .GroupBy(a => a.Specialty)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            
            stats.MostFrequentSpecialty = specialtyGroups?.Key ?? "No disponible";

            return stats;
        }

        public async Task<HealthDashboardViewModel> GetDashboardDataAsync(string userId)
        {
            var upcomingAppointments = await GetUpcomingAppointmentsAsync(userId, 7); // Próximos 7 días
            var activeMedications = await GetActiveMedicationsAsync(userId);
            var stats = await GetHealthStatsAsync(userId);

            return new HealthDashboardViewModel
            {
                UpcomingAppointments = upcomingAppointments,
                ActiveMedications = activeMedications,
                Stats = stats,
                AppointmentCount = stats.TotalAppointments,
                MedicationCount = activeMedications.Count
            };
        }

        // Reminders (para implementación futura)
        public async Task CheckAndSendRemindersAsync()
        {
            // Implementar lógica de recordatorios
            await Task.CompletedTask;
        }

        public async Task<List<MedicalAppointment>> GetAppointmentsNeedingReminderAsync()
        {
            // ✅ CORREGIDO: Usar UTC para PostgreSQL
            var reminderTime = DateTime.UtcNow.AddHours(24); // Recordatorios 24 horas antes
            var now = DateTime.UtcNow;

            return await _context.MedicalAppointments
                .Where(a => !a.ReminderSent &&
                           a.AppointmentDate <= reminderTime &&
                           a.AppointmentDate > now)
                .ToListAsync();
        }
    }
}