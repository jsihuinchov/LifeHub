using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LifeHub.Models.Services;
using LifeHub.Models.ViewModels;
using LifeHub.Models.Entities;

namespace LifeHub.Controllers
{
    [Authorize]
    public class WellnessController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHealthService _healthService;
        private readonly ILogger<WellnessController> _logger;

        public WellnessController(
            UserManager<IdentityUser> userManager,
            IHealthService healthService,
            ILogger<WellnessController> logger)
        {
            _userManager = userManager;
            _healthService = healthService;
            _logger = logger;
        }

        private string? GetUserId()
        {
            return _userManager.GetUserId(User);
        }

        // GET: Wellness/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var dashboard = await _healthService.GetWellnessDashboardAsync(userId);
                return View(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading wellness dashboard");
                TempData["ErrorMessage"] = "Error loading wellness dashboard.";
                return View(new WellnessDashboardViewModel());
            }
        }

        // GET: Wellness/Diary
        public async Task<IActionResult> Diary()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var todayCheck = await _healthService.GetTodayWellnessCheckAsync(userId);
                var model = new WellnessCheckViewModel();

                if (todayCheck != null)
                {
                    // ✅ CORREGIDO: Asignar propiedades directamente
                    model.Id = todayCheck.Id;
                    model.GeneralWellness = todayCheck.GeneralWellness;
                    model.SelectedSymptoms = todayCheck.GetSymptomsList();
                    model.CustomSymptom = todayCheck.CustomSymptom;
                    model.EnergyLevel = todayCheck.EnergyLevel;
                    model.SleepQuality = todayCheck.SleepQuality;
                    model.QuickNote = todayCheck.QuickNote;
                    model.TookMedications = todayCheck.TookMedications;
                    model.MedicationNotes = todayCheck.MedicationNotes;
                    model.HasEntryToday = true;
                    model.TodayEntry = todayCheck;
                }
                else
                {
                    model.HasEntryToday = false;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading wellness diary");
                TempData["ErrorMessage"] = "Error loading wellness diary.";
                return View(new WellnessCheckViewModel());
            }
        }

        // POST: Wellness/SaveDailyCheck
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDailyCheck(WellnessCheckViewModel model)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, error = "User not authenticated" });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, errors = errors });
                }

                var success = await _healthService.SaveWellnessCheckAsync(model, userId);

                if (success)
                {
                    return Json(new { 
                        success = true, 
                        message = "¡Día guardado exitosamente! ✅",
                        hasEntry = true
                    });
                }

                return Json(new { success = false, error = "Error guardando tu día" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving daily wellness check");
                return Json(new { success = false, error = "Error guardando tu día" });
            }
        }

        // GET: Wellness/History
        public async Task<IActionResult> History()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var history = await _healthService.GetWellnessHistoryAsync(userId, 30);
                
                var stats = new
                {
                    AverageEnergy = history.Any() ? Math.Round(history.Average(w => w.EnergyLevel), 1) : 0,
                    AverageSleep = history.Any() ? Math.Round(history.Average(w => w.SleepQuality), 1) : 0,
                    SymptomFrequency = history
                        .SelectMany(w => w.GetSymptomsList())
                        .GroupBy(s => s)
                        .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                    MostCommonWellness = history.Any() ? 
                        history.GroupBy(w => w.GeneralWellness)
                              .OrderByDescending(g => g.Count())
                              .First().Key : WellnessLevel.Regular
                };

                ViewBag.Stats = stats;
                return View(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading wellness history");
                TempData["ErrorMessage"] = "Error loading wellness history.";
                return View(new List<WellnessCheck>());
            }
        }

        // GET: Wellness/Insights
        public async Task<IActionResult> Insights()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var history = await _healthService.GetWellnessHistoryAsync(userId, 30);
                var insights = GenerateHealthInsights(history);

                return View(insights);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading wellness insights");
                TempData["ErrorMessage"] = "Error loading wellness insights.";
                return View(new List<string>());
            }
        }

        // GET: Wellness/GetTodayEntry
        [HttpGet]
        public async Task<IActionResult> GetTodayEntry()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, error = "User not authenticated" });
                }

                var todayCheck = await _healthService.GetTodayWellnessCheckAsync(userId);
                
                if (todayCheck == null)
                {
                    return Json(new { 
                        success = true, 
                        hasEntry = false,
                        message = "No hay entrada para hoy"
                    });
                }

                var model = new WellnessCheckViewModel
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

                return Json(new { 
                    success = true, 
                    hasEntry = true,
                    data = model
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's wellness entry");
                return Json(new { success = false, error = "Error obteniendo entrada de hoy" });
            }
        }

        // MÉTODO PRIVADO: Generar insights de salud
        private List<string> GenerateHealthInsights(List<WellnessCheck> history)
        {
            var insights = new List<string>();

            if (!history.Any()) 
            {
                insights.Add("Comienza a registrar tu bienestar diario para obtener insights personalizados.");
                return insights;
            }

            var avgEnergy = history.Average(w => w.EnergyLevel);
            if (avgEnergy < 5)
                insights.Add("💤 Tu energía promedio está baja. Considera mejorar tu descanso.");
            else if (avgEnergy >= 7)
                insights.Add("⚡ ¡Excelente nivel de energía! Sigue manteniendo tus buenos hábitos.");

            var avgSleep = history.Average(w => w.SleepQuality);
            if (avgSleep < 5)
                insights.Add("🌙 Tu calidad de sueño puede mejorar. Intenta establecer una rutina de descanso.");

            var frequentSymptoms = history
                .SelectMany(w => w.GetSymptomsList())
                .GroupBy(s => s)
                .Where(g => g.Count() >= 3)
                .OrderByDescending(g => g.Count())
                .Take(3);

            foreach (var symptomGroup in frequentSymptoms)
            {
                var symptomName = new WellnessCheck().GetSymptomDisplayName(symptomGroup.Key);
                insights.Add($"🔍 {symptomName} aparece frecuentemente ({symptomGroup.Count()} veces)");
            }

            var wellnessDistribution = history
                .GroupBy(w => w.GeneralWellness)
                .OrderByDescending(g => g.Count())
                .First();

            insights.Add($"📊 Tu estado más común es: {wellnessDistribution.Key} ({wellnessDistribution.Count()} días)");

            return insights;
        }
    }
}