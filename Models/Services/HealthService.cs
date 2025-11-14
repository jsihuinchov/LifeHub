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
        private readonly IPatternDetectionService _patternDetectionService;
        private readonly IAppointmentChatbotService _chatbotService;
        private readonly IMedicalReportService _reportService;

        public HealthService(
            ApplicationDbContext context,
            ILogger<HealthService> logger,
            IMedicationApiService medicationApiService,
            IPatternDetectionService patternDetectionService,
            IAppointmentChatbotService chatbotService,
            IMedicalReportService reportService)
        {
            _context = context;
            _logger = logger;
            _medicationApiService = medicationApiService;
            _patternDetectionService = patternDetectionService;
            _chatbotService = chatbotService;
            _reportService = reportService;
        }

        // === MÉTODOS DE MEDICAMENTOS (MANTENER) ===
        public async Task<List<MedicationSuggestion>> SearchMedicationsAsync(string searchTerm)
        {
            try
            {
                var results = await _medicationApiService.SearchMedicationsAsync(searchTerm);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SearchMedicationsAsync");
                return new List<MedicationSuggestion>();
            }
        }

        public async Task<MedicationInfo?> GetMedicationDetailsAsync(string medicationId)
        {
            return await _medicationApiService.GetMedicationDetailsAsync(medicationId);
        }

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
                var medication = new Medication
                {
                    UserId = userId,
                    Name = model.Name,
                    Dosage = model.Dosage,
                    Frequency = model.Frequency,
                    StartDate = model.StartDate?.ToUniversalTime(),
                    EndDate = model.EndDate?.ToUniversalTime(),
                    Notes = model.Notes,
                    IsActive = model.IsActive,
                    TotalQuantity = model.TotalQuantity,
                    DosagePerIntake = model.DosagePerIntake,
                    TimesPerDay = model.TimesPerDay,
                    LowStockAlert = model.LowStockAlert,
                    RequiresPrescription = model.RequiresPrescription,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Medications.Add(medication);
                await _context.SaveChangesAsync();
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

                medication.Name = model.Name;
                medication.Dosage = model.Dosage;
                medication.Frequency = model.Frequency;
                medication.StartDate = model.StartDate?.ToUniversalTime();
                medication.EndDate = model.EndDate?.ToUniversalTime();
                medication.Notes = model.Notes;
                medication.IsActive = model.IsActive;
                medication.TotalQuantity = model.TotalQuantity;
                medication.DosagePerIntake = model.DosagePerIntake;
                medication.TimesPerDay = model.TimesPerDay;
                medication.LowStockAlert = model.LowStockAlert;
                medication.RequiresPrescription = model.RequiresPrescription;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar medicamento {MedicationId}", model.Id);
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
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar medicamento {MedicationId}", id);
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
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado de medicamento {MedicationId}", id);
                return false;
            }
        }

        // === MÉTODOS DE CITAS MÉDICAS ===
        public async Task<List<MedicalAppointment>> GetUserAppointmentsAsync(string userId)
        {
            return await _context.MedicalAppointments
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AppointmentDate)
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
                var appointment = new MedicalAppointment
                {
                    UserId = userId,
                    Title = model.Title,
                    DoctorName = model.DoctorName,
                    Specialty = model.Specialty,
                    AppointmentDate = model.AppointmentDate.ToUniversalTime(),
                    Duration = model.Duration,
                    Location = model.Location,
                    Notes = model.Notes,
                    HealthConcerns = model.HealthConcerns,
                    QuestionsForDoctor = model.QuestionsForDoctor,
                    ReminderSent = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.MedicalAppointments.Add(appointment);
                await _context.SaveChangesAsync();
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

                appointment.Title = model.Title;
                appointment.DoctorName = model.DoctorName;
                appointment.Specialty = model.Specialty;
                appointment.AppointmentDate = model.AppointmentDate.ToUniversalTime();
                appointment.Duration = model.Duration;
                appointment.Location = model.Location;
                appointment.Notes = model.Notes;
                appointment.HealthConcerns = model.HealthConcerns;
                appointment.QuestionsForDoctor = model.QuestionsForDoctor;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar cita médica {AppointmentId}", model.Id);
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
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar cita médica {AppointmentId}", id);
                return false;
            }
        }

        // === MÉTODOS DE WELLNESS CHECK ===
        public async Task<bool> SaveWellnessCheckAsync(WellnessCheckViewModel model, string userId)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var existingCheck = await _context.WellnessChecks
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.CheckDate == today);
                    
                if (existingCheck != null)
                {
                    existingCheck.GeneralWellness = model.GeneralWellness;
                    existingCheck.EnergyLevel = model.EnergyLevel;
                    existingCheck.SleepQuality = model.SleepQuality;
                    existingCheck.QuickNote = model.QuickNote;
                    existingCheck.TookMedications = model.TookMedications;
                    existingCheck.MedicationNotes = model.MedicationNotes;
                    existingCheck.CustomSymptom = model.CustomSymptom;
                    existingCheck.SetSymptomsList(model.SelectedSymptoms);
                }
                else
                {
                    var check = new WellnessCheck
                    {
                        UserId = userId,
                        CheckDate = today,
                        GeneralWellness = model.GeneralWellness,
                        EnergyLevel = model.EnergyLevel,
                        SleepQuality = model.SleepQuality,
                        QuickNote = model.QuickNote,
                        TookMedications = model.TookMedications,
                        MedicationNotes = model.MedicationNotes,
                        CustomSymptom = model.CustomSymptom,
                        CreatedAt = DateTime.UtcNow
                    };
                    check.SetSymptomsList(model.SelectedSymptoms);
                    _context.WellnessChecks.Add(check);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving wellness check for user {UserId}", userId);
                return false;
            }
        }

        public async Task<WellnessCheck?> GetTodayWellnessCheckAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            return await _context.WellnessChecks
                .FirstOrDefaultAsync(w => w.UserId == userId && w.CheckDate == today);
        }

        public async Task<List<WellnessCheck>> GetWellnessHistoryAsync(string userId, int days = 7)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);
            return await _context.WellnessChecks
                .Where(w => w.UserId == userId && w.CheckDate >= startDate)
                .OrderByDescending(w => w.CheckDate)
                .ToListAsync();
        }

        public async Task<WellnessDashboardViewModel> GetWellnessDashboardAsync(string userId)
        {
            var todayCheck = await GetTodayWellnessCheckAsync(userId);
            var last7Days = await GetWellnessHistoryAsync(userId, 7);
            var activeMeds = await GetActiveMedicationsAsync(userId);
            var upcomingAppointments = await GetUpcomingAppointmentsByUserIdAsync(userId, 7); // ✅ Cambiado el nombre

            var lowStockMeds = activeMeds
                .Where(m => m.TotalQuantity <= m.LowStockAlert)
                .ToList();

            var currentStreak = CalculateCurrentStreak(last7Days);
            var averageEnergy = last7Days.Any() ? last7Days.Average(w => w.EnergyLevel) : 0;
            var averageSleep = last7Days.Any() ? last7Days.Average(w => w.SleepQuality) : 0;

            var symptomFrequency = last7Days
                .SelectMany(w => w.GetSymptomsList())
                .GroupBy(s => s)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            var mostCommonWellness = last7Days.Any() ?
                last7Days.GroupBy(w => w.GeneralWellness)
                        .OrderByDescending(g => g.Count())
                        .First().Key : WellnessLevel.Regular;

            WellnessCheckViewModel? todayCheckViewModel = null;
            if (todayCheck != null)
            {
                todayCheckViewModel = new WellnessCheckViewModel
                {
                    Id = todayCheck.Id,
                    GeneralWellness = todayCheck.GeneralWellness,
                    SelectedSymptoms = todayCheck.GetSymptomsList(),
                    CustomSymptom = todayCheck.CustomSymptom,
                    EnergyLevel = todayCheck.EnergyLevel,
                    SleepQuality = todayCheck.SleepQuality,
                    QuickNote = todayCheck.QuickNote,
                    TookMedications = todayCheck.TookMedications,
                    MedicationNotes = todayCheck.MedicationNotes,
                    HasEntryToday = true,
                    TodayEntry = todayCheck
                };
            }

            return new WellnessDashboardViewModel
            {
                TodayCheck = todayCheckViewModel,
                Last7Days = last7Days,
                ActiveMedications = activeMeds,
                UpcomingAppointments = upcomingAppointments,
                LowStockMedications = lowStockMeds,
                CurrentStreak = currentStreak,
                AverageEnergy = Math.Round(averageEnergy, 1),
                AverageSleepQuality = Math.Round(averageSleep, 1),
                MostCommonWellness = mostCommonWellness,
                SymptomFrequency = symptomFrequency,
                MostCommonMood = "neutral",
                HealthInsights = GenerateHealthInsights(last7Days)
            };
        }

        private int CalculateCurrentStreak(List<WellnessCheck> checks)
        {
            if (!checks.Any()) return 0;

            var sortedDates = checks.Select(c => c.CheckDate).OrderByDescending(d => d).ToList();
            var streak = 0;
            var currentDate = DateTime.UtcNow.Date;

            foreach (var date in sortedDates)
            {
                if (date == currentDate)
                {
                    streak++;
                    currentDate = currentDate.AddDays(-1);
                }
                else
                {
                    break;
                }
            }

            return streak;
        }

        private List<string> GenerateHealthInsights(List<WellnessCheck> checks)
        {
            var insights = new List<string>();

            if (!checks.Any()) 
            {
                insights.Add("Comienza a registrar tu bienestar diario para obtener insights personalizados.");
                return insights;
            }

            var avgEnergy = checks.Average(w => w.EnergyLevel);
            var avgSleep = checks.Average(w => w.SleepQuality);

            if (avgEnergy < 5) insights.Add("💤 Tu energía promedio está baja. Considera mejorar tu descanso.");
            if (avgSleep < 5) insights.Add("🌙 Tu calidad de sueño puede mejorar. Intenta establecer una rutina de descanso.");

            var frequentSymptoms = checks
                .SelectMany(w => w.GetSymptomsList())
                .GroupBy(s => s)
                .Where(g => g.Count() >= 2)
                .OrderByDescending(g => g.Count())
                .Take(3);

            foreach (var symptomGroup in frequentSymptoms)
            {
                var symptomName = new WellnessCheck().GetSymptomDisplayName(symptomGroup.Key);
                insights.Add($"🔍 {symptomName} aparece frecuentemente");
            }

            return insights;
        }

        // === MÉTODOS AUXILIARES - SIN DUPLICADOS ===
        
        // ✅ MÉTODO ORIGINAL (por userId)
        public async Task<List<MedicalAppointment>> GetUpcomingAppointmentsByUserIdAsync(string userId, int days = 7)
        {
            var startDate = DateTime.UtcNow.Date;
            var endDate = startDate.AddDays(days);
            
            return await _context.MedicalAppointments
                .Where(a => a.UserId == userId && 
                           a.AppointmentDate >= startDate && 
                           a.AppointmentDate <= endDate)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();
        }

        // ✅ MÉTODO POR EMAIL (implementación separada)
        public async Task<List<MedicalAppointment>> GetUpcomingAppointmentsAsync(string userEmail, int daysAhead = 7)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null) return new List<MedicalAppointment>();
            
            return await GetUpcomingAppointmentsByUserIdAsync(user.Id, daysAhead);
        }

        public async Task<List<Medication>> GetMedicationsNeedingRefillAsync(string userId)
        {
            return await _context.Medications
                .Where(m => m.UserId == userId && m.IsActive && m.TotalQuantity <= m.LowStockAlert)
                .ToListAsync();
        }

        // === IMPLEMENTACIÓN DE INTERFAZ ===
        public async Task<HealthStatsViewModel> GetHealthStatsAsync(string userId)
        {
            var appointments = await GetUserAppointmentsAsync(userId);
            var medications = await GetUserMedicationsAsync(userId);
            
            return new HealthStatsViewModel
            {
                TotalAppointments = appointments.Count,
                UpcomingAppointments = appointments.Count(a => a.AppointmentDate >= DateTime.UtcNow),
                ActiveMedications = medications.Count(m => m.IsActive),
                CompletedAppointments = appointments.Count(a => a.AppointmentDate < DateTime.UtcNow),
                AverageAppointmentsPerMonth = 0,
                MostFrequentSpecialty = string.Empty
            };
        }

        public async Task<HealthDashboardViewModel> GetDashboardDataAsync(string userId)
        {
            var upcomingAppointments = await GetUpcomingAppointmentsByUserIdAsync(userId, 7); // ✅ Usar método renombrado
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

        public async Task<List<Medication>> GetTodayMedicationsAsync(string userId)
        {
            return await GetActiveMedicationsAsync(userId);
        }

        public async Task CheckAndSendRemindersAsync()
        {
            await Task.CompletedTask;
        }

        public async Task<List<MedicalAppointment>> GetAppointmentsNeedingReminderAsync()
        {
            return new List<MedicalAppointment>();
        }

        public async Task<WeeklyHealthStats> GetWeeklyHealthStatsAsync(string userId)
        {
            var checks = await GetWellnessHistoryAsync(userId, 7);
            var medications = await GetActiveMedicationsAsync(userId);

            return new WeeklyHealthStats
            {
                AverageMood = checks.Any() ? checks.Average(w => (int)w.GeneralWellness) : 0,
                AverageEnergy = checks.Any() ? checks.Average(w => w.EnergyLevel) : 0,
                MedicationsTaken = checks.Count(w => w.TookMedications),
                TotalMedications = medications.Count,
                AppointmentsThisWeek = 0,
                Recommendations = "Sigue registrando tu bienestar para obtener recomendaciones personalizadas."
            };
        }

        public async Task<List<Medication>> GetMedicationsByUserAsync(string userEmail)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null) return new List<Medication>();
            
            return await GetActiveMedicationsAsync(user.Id);
        }

        public async Task<List<HealthPattern>> DetectHealthPatternsAsync(string userId)
        {
            try
            {
                return await _patternDetectionService.AnalyzePatternsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting health patterns for user {UserId}", userId);
                return new List<HealthPattern>();
            }
        }

        public async Task<AppointmentPreparation> GenerateAppointmentPreparationAsync(int appointmentId)
        {
            try
            {
                return await _chatbotService.PrepareForAppointmentAsync(appointmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating appointment preparation for {AppointmentId}", appointmentId);
                return new AppointmentPreparation();
            }
        }

        public async Task<MedicalReport> GenerateMedicalReportAsync(string userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var range = new DateRange { StartDate = startDate, EndDate = endDate };
                return await _reportService.GenerateReportAsync(userId, range);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating medical report for user {UserId}", userId);
                return new MedicalReport();
            }
        }

        public async Task<List<string>> GetPredictiveInsightsAsync(string userId)
        {
            try
            {
                return await _patternDetectionService.GeneratePredictiveInsightsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting predictive insights for user {UserId}", userId);
                return new List<string> { "Continúa registrando datos para obtener insights personalizados." };
            }
        }
    }
}