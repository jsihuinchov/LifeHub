using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LifeHub.Models.Services;
using LifeHub.Models.ViewModels;
using LifeHub.Models.Entities;

namespace LifeHub.Controllers
{
    [Authorize]
    public class HealthController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHealthService _healthService;
        private readonly ILogger<HealthController> _logger;
        private readonly IMedicationApiService _medicationApiService;
        private readonly IChatbotService _chatbotService;

        public HealthController(
            UserManager<IdentityUser> userManager,
            IHealthService healthService,
            IMedicationApiService medicationApiService,
            IChatbotService chatbotService,
            ILogger<HealthController> logger)
        {
            _userManager = userManager;
            _healthService = healthService;
            _medicationApiService = medicationApiService;
            _chatbotService = chatbotService;
            _logger = logger;
        }

        private string? GetUserId()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("No se pudo obtener el ID del usuario autenticado");
            }
            return userId;
        }

        // ========== DASHBOARD Y DIARIO ==========

        // GET: Health/Index - Dashboard principal
        public async Task<IActionResult> Index()
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

        // GET: Health/Diary - Diario de salud
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

        // POST: Health/SaveDailyCheck
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

        // ========== HISTORIAL E INSIGHTS ==========

        // GET: Health/History
        public async Task<IActionResult> History(int days = 30)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var history = await _healthService.GetWellnessHistoryAsync(userId, days);
                
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

        // GET: Health/Insights
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

        // ========== MEDICAMENTOS ==========

        // GET: Health/Medications - Lista de medicamentos
        public async Task<IActionResult> Medications()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var medications = await _healthService.GetUserMedicationsAsync(userId);
                return View(medications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar los medicamentos");
                TempData["ErrorMessage"] = "Error al cargar los medicamentos.";
                return View(new List<Medication>());
            }
        }

        // GET: Health/CreateMedication
        public IActionResult CreateMedication()
        {
            return View(new MedicationViewModel());
        }

        // POST: Health/CreateMedication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMedication(MedicationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var success = await _healthService.CreateMedicationAsync(model, userId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Medicamento creado exitosamente.";
                    return RedirectToAction(nameof(Medications));
                }

                TempData["ErrorMessage"] = "Error al crear el medicamento.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear medicamento");
                TempData["ErrorMessage"] = "Error al crear el medicamento.";
                return View(model);
            }
        }

        // GET: Health/EditMedication/5
        public async Task<IActionResult> EditMedication(int id)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var medication = await _healthService.GetMedicationByIdAsync(id, userId);

                if (medication == null)
                {
                    TempData["ErrorMessage"] = "Medicamento no encontrado.";
                    return RedirectToAction(nameof(Medications));
                }

                var model = new MedicationViewModel
                {
                    Id = medication.Id,
                    Name = medication.Name,
                    Dosage = medication.Dosage,
                    Frequency = medication.Frequency,
                    StartDate = medication.StartDate,
                    EndDate = medication.EndDate,
                    Notes = medication.Notes,
                    IsActive = medication.IsActive,
                    TotalQuantity = medication.TotalQuantity,
                    DosagePerIntake = medication.DosagePerIntake,
                    TimesPerDay = medication.TimesPerDay,
                    LowStockAlert = medication.LowStockAlert,
                    RequiresPrescription = medication.RequiresPrescription
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar medicamento para editar");
                TempData["ErrorMessage"] = "Error al cargar el medicamento.";
                return RedirectToAction(nameof(Medications));
            }
        }

        // POST: Health/EditMedication/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMedication(int id, MedicationViewModel model)
        {
            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "ID de medicamento no coincide.";
                return RedirectToAction(nameof(Medications));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var success = await _healthService.UpdateMedicationAsync(model, userId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Medicamento actualizado exitosamente.";
                    return RedirectToAction(nameof(Medications));
                }

                TempData["ErrorMessage"] = "Error al actualizar el medicamento.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar medicamento");
                TempData["ErrorMessage"] = "Error al actualizar el medicamento.";
                return View(model);
            }
        }

        // POST: Health/DeleteMedication/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMedication(int id)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var success = await _healthService.DeleteMedicationAsync(id, userId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Medicamento eliminado exitosamente.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al eliminar el medicamento.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar medicamento");
                TempData["ErrorMessage"] = "Error al eliminar el medicamento.";
            }

            return RedirectToAction(nameof(Medications));
        }

        // POST: Health/ToggleMedicationStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMedicationStatus(int id)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var success = await _healthService.ToggleMedicationStatusAsync(id, userId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Estado del medicamento actualizado exitosamente.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al actualizar el estado del medicamento.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado de medicamento");
                TempData["ErrorMessage"] = "Error al actualizar el estado del medicamento.";
            }

            return RedirectToAction(nameof(Medications));
        }

        // ========== CITAS MÉDICAS ==========

        // GET: Health/Appointments - Lista de citas médicas
        public async Task<IActionResult> Appointments()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var appointments = await _healthService.GetUserAppointmentsAsync(userId);
                return View(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar las citas médicas");
                TempData["ErrorMessage"] = "Error al cargar las citas médicas.";
                return View(new List<MedicalAppointment>());
            }
        }

        // GET: Health/CreateAppointment
        public IActionResult CreateAppointment()
        {
            return View(new MedicalAppointmentViewModel());
        }

        // POST: Health/CreateAppointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAppointment(MedicalAppointmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var success = await _healthService.CreateAppointmentAsync(model, userId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Cita médica creada exitosamente.";
                    return RedirectToAction(nameof(Appointments));
                }

                TempData["ErrorMessage"] = "Error al crear la cita médica.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear cita médica");
                TempData["ErrorMessage"] = "Error al crear la cita médica.";
                return View(model);
            }
        }

        // GET: Health/EditAppointment/5
        public async Task<IActionResult> EditAppointment(int id)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var appointment = await _healthService.GetAppointmentByIdAsync(id, userId);

                if (appointment == null)
                {
                    TempData["ErrorMessage"] = "Cita médica no encontrada.";
                    return RedirectToAction(nameof(Appointments));
                }

                var model = new MedicalAppointmentViewModel
                {
                    Id = appointment.Id,
                    Title = appointment.Title,
                    DoctorName = appointment.DoctorName,
                    Specialty = appointment.Specialty,
                    AppointmentDate = appointment.AppointmentDate,
                    Duration = appointment.Duration,
                    Location = appointment.Location,
                    Notes = appointment.Notes,
                    HealthConcerns = appointment.HealthConcerns,
                    QuestionsForDoctor = appointment.QuestionsForDoctor,
                    ReminderSent = appointment.ReminderSent
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar cita médica para editar");
                TempData["ErrorMessage"] = "Error al cargar la cita médica.";
                return RedirectToAction(nameof(Appointments));
            }
        }

        // POST: Health/EditAppointment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAppointment(int id, MedicalAppointmentViewModel model)
        {
            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "ID de cita médica no coincide.";
                return RedirectToAction(nameof(Appointments));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var success = await _healthService.UpdateAppointmentAsync(model, userId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Cita médica actualizada exitosamente.";
                    return RedirectToAction(nameof(Appointments));
                }

                TempData["ErrorMessage"] = "Error al actualizar la cita médica.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar cita médica");
                TempData["ErrorMessage"] = "Error al actualizar la cita médica.";
                return View(model);
            }
        }

        // POST: Health/DeleteAppointment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var success = await _healthService.DeleteAppointmentAsync(id, userId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Cita médica eliminada exitosamente.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al eliminar la cita médica.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar cita médica");
                TempData["ErrorMessage"] = "Error al eliminar la cita médica.";
            }

            return RedirectToAction(nameof(Appointments));
        }

        // ========== CHATBOT Y ASISTENTE ==========

        [HttpGet]
        public async Task<IActionResult> Chatbot(int? appointmentId = null)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                ViewBag.AppointmentId = appointmentId;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chatbot");
                TempData["ErrorMessage"] = "Error loading chatbot.";
                return View();
            }
        }

        [HttpPost]
        public async Task<JsonResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, error = "User not authenticated" });
                }

                _logger.LogInformation("💬 [CONTROLLER] Procesando mensaje: {Message}", request.Message);
                var response = await _chatbotService.ProcessMessageAsync(userId, request.Message);

                _logger.LogInformation("🤖 [CONTROLLER] Respuesta generada: {ResponseLength} chars", response.Message.Length);
                return Json(new { success = true, response = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [CONTROLLER] Error sending chatbot message");
                return Json(new { 
                    success = false, 
                    error = "Error processing message",
                    response = new ChatResponse 
                    { 
                        Message = "Lo siento, estoy teniendo problemas técnicos. Por favor, intenta de nuevo en un momento.",
                        QuickActions = new List<QuickAction>
                        {
                            new() { Text = "🔄 Reintentar", Action = "retry" },
                            new() { Text = "🏠 Volver al inicio", Action = "go_home" }
                        }
                    }
                });
            }
        }

        [HttpPost]
        public async Task<JsonResult> StartAppointmentPreparation([FromBody] StartAppointmentRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, error = "User not authenticated" });
                }

                _logger.LogInformation("📅 [CONTROLLER] Iniciando preparación para cita: {AppointmentId}", request.AppointmentId);
                var response = await _chatbotService.StartAppointmentPreparationAsync(request.AppointmentId);

                _logger.LogInformation("✅ [CONTROLLER] Preparación iniciada exitosamente");
                return Json(new { success = true, response = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [CONTROLLER] Error starting appointment preparation");
                return Json(new { 
                    success = false, 
                    error = "Error starting preparation",
                    response = new ChatResponse 
                    { 
                        Message = "No pude iniciar la preparación de la cita. Verifica que la cita exista y esté disponible.",
                        QuickActions = new List<QuickAction>
                        {
                            new() { Text = "📅 Ver mis citas", Action = "view_appointments" },
                            new() { Text = "🔄 Reintentar", Action = "retry" }
                        }
                    }
                });
            }
        }

        // ========== MÉTODOS AUXILIARES ==========

        // GET: Health/GetTodayEntry
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

        // GET: Health/Stats - Estadísticas de salud
        public async Task<IActionResult> Stats()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var stats = await _healthService.GetHealthStatsAsync(userId);
                return View(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar estadísticas de salud");
                TempData["ErrorMessage"] = "Error al cargar las estadísticas de salud.";
                return View(new HealthStatsViewModel());
            }
        }

        // ========== MÉTODOS DE BÚSQUEDA DE MEDICAMENTOS ==========

        [HttpGet]
        public async Task<IActionResult> SearchMedications(string term)
        {
            _logger.LogInformation("🔍 [CONTROLLER] Búsqueda RxNorm: {Term}", term);

            try
            {
                if (string.IsNullOrWhiteSpace(term))
                {
                    return Json(new { success = false, error = "Ingresa un término de búsqueda" });
                }

                var results = await _healthService.SearchMedicationsAsync(term);
                _logger.LogInformation("✅ [CONTROLLER] RxNorm encontró {Count} medicamentos", results.Count);

                return Json(new
                {
                    success = true,
                    results = results,
                    source = "RxNorm NIH - Base de Datos Global",
                    count = results.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [CONTROLLER] Error en búsqueda RxNorm");
                return Json(new
                {
                    success = false,
                    error = "Error en la búsqueda",
                    results = new List<object>()
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMedicationDetails(string id)
        {
            try
            {
                var details = await _healthService.GetMedicationDetailsAsync(id);
                return Json(new { success = true, details });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalles del medicamento: {Id}", id);
                return Json(new { success = false, error = "Error al obtener detalles" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestFDASearch()
        {
            try
            {
                _logger.LogInformation("🧪 TEST: Probando FDA Search Service");

                var testTerm = "aspirin";
                var results = await _healthService.SearchMedicationsAsync(testTerm);

                _logger.LogInformation("🧪 TEST: Resultados para '{Term}': {Count}", testTerm, results.Count);

                foreach (var result in results)
                {
                    _logger.LogInformation("🧪 TEST Medicamento: {Name} - {Strength} - {Manufacturer}",
                        result.Name, result.Strength, result.Manufacturer);
                }

                return Json(new
                {
                    success = true,
                    testTerm = testTerm,
                    resultCount = results.Count,
                    results = results,
                    message = "Test completado - revisa los logs"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🧪 TEST: Error en test FDA");
                return Json(new
                {
                    success = false,
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestApiConnection()
        {
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync("https://api.fda.gov/drug/label.json?search=openfda.brand_name:aspirin&limit=1");

                return Json(new
                {
                    canReachFDA = response.IsSuccessStatusCode,
                    statusCode = response.StatusCode,
                    isUsingRealAPI = true,
                    message = response.IsSuccessStatusCode ?
                        "✅ Conectado a FDA API - Usando datos reales" :
                        "❌ No se puede conectar a FDA API - Usando fallback"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    canReachFDA = false,
                    error = ex.Message,
                    isUsingRealAPI = false,
                    message = "❌ Error de conexión - Usando datos locales"
                });
            }
        }

        // ========== ANÁLISIS AVANZADO ==========

        [HttpGet]
        public async Task<IActionResult> HealthPatterns()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var patterns = await _healthService.DetectHealthPatternsAsync(userId);
                return View(patterns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading health patterns");
                TempData["ErrorMessage"] = "Error loading health patterns.";
                return View(new List<HealthPattern>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> AppointmentPreparation(int id)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var appointment = await _healthService.GetAppointmentByIdAsync(id, userId);
                if (appointment == null)
                {
                    TempData["ErrorMessage"] = "Cita médica no encontrada.";
                    return RedirectToAction(nameof(Appointments));
                }

                var preparation = await _healthService.GenerateAppointmentPreparationAsync(id);
                ViewBag.Appointment = appointment;
                
                return View(preparation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating appointment preparation");
                TempData["ErrorMessage"] = "Error generating appointment preparation.";
                return RedirectToAction(nameof(Appointments));
            }
        }

        [HttpGet]
        public async Task<IActionResult> MedicalReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var defaultStart = startDate ?? DateTime.UtcNow.AddDays(-30);
                var defaultEnd = endDate ?? DateTime.UtcNow;

                var report = await _healthService.GenerateMedicalReportAsync(userId, defaultStart, defaultEnd);
                
                ViewBag.StartDate = defaultStart;
                ViewBag.EndDate = defaultEnd;
                
                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating medical report");
                TempData["ErrorMessage"] = "Error generating medical report.";
                return View(new MedicalReport());
            }
        }

        [HttpGet]
        public async Task<IActionResult> PredictiveInsights()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var insights = await _healthService.GetPredictiveInsightsAsync(userId);
                return View(insights);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading predictive insights");
                TempData["ErrorMessage"] = "Error loading predictive insights.";
                return View(new List<string>());
            }
        }

        // API Endpoints para AJAX
        [HttpGet]
        public async Task<IActionResult> GetAppointmentPreparation(int appointmentId)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, error = "User not authenticated" });
                }

                // Verificar que la cita pertenece al usuario
                var appointment = await _healthService.GetAppointmentByIdAsync(appointmentId, userId);
                if (appointment == null)
                {
                    return Json(new { success = false, error = "Appointment not found" });
                }

                var preparation = await _healthService.GenerateAppointmentPreparationAsync(appointmentId);
                
                return Json(new { 
                    success = true, 
                    preparation = preparation 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting appointment preparation");
                return Json(new { success = false, error = "Error generating preparation" });
            }
        }

        // ========== MÉTODOS PRIVADOS ==========

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
                var symptomName = GetSymptomDisplayName(symptomGroup.Key);
                insights.Add($"🔍 {symptomName} aparece frecuentemente ({symptomGroup.Count()} veces)");
            }

            var wellnessDistribution = history
                .GroupBy(w => w.GeneralWellness)
                .OrderByDescending(g => g.Count())
                .First();

            insights.Add($"📊 Tu estado más común es: {wellnessDistribution.Key} ({wellnessDistribution.Count()} días)");

            return insights;
        }

        // MÉTODO PRIVADO: Obtener nombre display del síntoma
        private string GetSymptomDisplayName(HealthSymptom symptom)
        {
            return symptom switch
            {
                HealthSymptom.Headache => "🤕 Dolor de cabeza",
                HealthSymptom.Fatigue => "😴 Fatiga",
                HealthSymptom.Nausea => "🤢 Náuseas",
                HealthSymptom.Palpitations => "💓 Palpitaciones",
                HealthSymptom.Fever => "🤒 Fiebre",
                HealthSymptom.LossOfAppetite => "🍽️ Falta de apetito",
                HealthSymptom.Anxiety => "😰 Ansiedad",
                HealthSymptom.Depression => "😞 Depresión",
                HealthSymptom.SleepProblems => "💤 Problemas de sueño",
                HealthSymptom.JointPain => "🦵 Dolor articular",
                HealthSymptom.Dizziness => "🌀 Mareos",
                HealthSymptom.Other => "📝 Otro",
                _ => symptom.ToString()
            };
        }
    }

    // ========== MODELOS DE SOLICITUD ==========

    public class ChatMessageRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class StartAppointmentRequest
    {
        public int AppointmentId { get; set; }
    }
}