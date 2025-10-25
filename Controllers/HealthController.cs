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

        public HealthController(
            UserManager<IdentityUser> userManager,
            IHealthService healthService,
            ILogger<HealthController> logger)
        {
            _userManager = userManager;
            _healthService = healthService;
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

                var dashboardData = await _healthService.GetDashboardDataAsync(userId);
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el dashboard de salud");
                TempData["ErrorMessage"] = "Error al cargar el dashboard de salud.";
                return View(new HealthDashboardViewModel());
            }
        }

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
                    IsActive = medication.IsActive
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

                // Test directo con un término conocido
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
                // Test directo a FDA API
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
    }
}