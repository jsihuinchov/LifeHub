using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using LifeHub.Models.Entities;
using LifeHub.Models.ViewModels;
using LifeHub.Services;
using LifeHub.Models.IA.Services;
using LifeHub.Models.IA.Results;

namespace LifeHub.Controllers
{
    [Authorize]
    public class HabitsController : Controller
    {
        private readonly IHabitService _habitService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IHabitAIService _habitAIService;

        public HabitsController(
            IHabitService habitService, 
            UserManager<IdentityUser> userManager, 
            ISubscriptionService subscriptionService,
            IHabitAIService habitAIService)
        {
            _habitService = habitService;
            _userManager = userManager;
            _subscriptionService = subscriptionService;
            _habitAIService = habitAIService;
        }

        private string GetUserId() => _userManager.GetUserId(User)!;

        // GET: Habits
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var habits = await _habitService.GetUserHabitsAsync(userId);
            var stats = await _habitService.GetHabitStatsAsync(userId);
            
            // Obtener información del plan del usuario
            var userSubscription = await _subscriptionService.GetUserSubscriptionAsync(userId);
            var hasAIFeatures = userSubscription?.Plan?.HasAIFeatures == true;
            
            // ✅ CARGAR DATOS DE IA SI EL USUARIO TIENE ACCESO
            if (hasAIFeatures)
            {
                try
                {
                    var aiRecommendations = await _habitAIService.GeneratePersonalizedRecommendationsAsync(userId);
                    var consistencyAlerts = await _habitAIService.GetConsistencyAlertsAsync(userId);
                    var habitPatterns = await _habitAIService.DetectHabitPatternsAsync(userId);

                    ViewBag.AIRecommendations = aiRecommendations;
                    ViewBag.ConsistencyAlerts = consistencyAlerts;
                    ViewBag.HabitPatterns = habitPatterns;
                }
                catch (Exception ex)
                {
                    // Si hay error en IA, mostrar versión básica
                    Console.WriteLine($"Error en IA: {ex.Message}");
                    ViewBag.AIRecommendations = new List<HabitRecommendation>();
                    ViewBag.ConsistencyAlerts = new List<ConsistencyAlert>();
                    ViewBag.HabitPatterns = new List<HabitPattern>();
                }
            }
            
            ViewBag.HasAIFeatures = hasAIFeatures;
            ViewBag.Stats = stats;
            ViewBag.UserSubscription = userSubscription;
            
            return View(habits);
        }

        // GET: Habits/Create
        public IActionResult Create()
        {
            var model = new HabitViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HabitViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validación de límite de plan
            var subscription = await _subscriptionService.GetUserSubscriptionAsync(GetUserId());
            if (subscription?.Plan == null)
            {
                ModelState.AddModelError("", "No tienes un plan activo. Por favor, selecciona un plan.");
                return View(model);
            }

            var currentHabits = await _habitService.GetUserHabitsAsync(GetUserId());
            if (currentHabits.Count >= subscription.Plan.MaxHabits)
            {
                ModelState.AddModelError("",
                    $"Has alcanzado el límite de {subscription.Plan.MaxHabits} hábitos de tu plan {subscription.Plan.Name}.");
                return View(model);
            }

            var habit = new Habit
            {
                Name = model.Name,
                Description = model.Description,
                Category = model.Category,
                Frequency = model.Frequency,
                TargetCount = model.TargetCount,
                ColorCode = model.ColorCode,
                Icon = model.Icon,
                StartDate = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _habitService.CreateHabitAsync(habit, GetUserId());

            if (result)
            {
                TempData["SuccessMessage"] = "¡Hábito creado exitosamente!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError("", "Error interno al crear el hábito. Por favor, intenta nuevamente.");
                return View(model);
            }
        }

        // GET: Habits/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var habit = await _habitService.GetHabitByIdAsync(id, GetUserId());
            if (habit == null)
            {
                return NotFound();
            }

            var model = new HabitViewModel
            {
                Id = habit.Id,
                Name = habit.Name,
                Description = habit.Description,
                Category = habit.Category,
                Frequency = habit.Frequency,
                TargetCount = habit.TargetCount,
                ColorCode = habit.ColorCode,
                Icon = habit.Icon
            };

            return View(model);
        }

        // POST: Habits/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, HabitViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var habit = new Habit
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                Category = model.Category,
                Frequency = model.Frequency,
                TargetCount = model.TargetCount,
                ColorCode = model.ColorCode,
                Icon = model.Icon
            };

            var result = await _habitService.UpdateHabitAsync(habit, GetUserId());
            
            if (result)
            {
                TempData["SuccessMessage"] = "¡Hábito actualizado exitosamente!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError("", "No se pudo actualizar el hábito.");
                return View(model);
            }
        }

        // POST: Habits/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _habitService.DeleteHabitAsync(id, GetUserId());
            
            if (result)
            {
                TempData["SuccessMessage"] = "¡Hábito eliminado exitosamente!";
            }
            else
            {
                TempData["ErrorMessage"] = "No se pudo eliminar el hábito.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Habits/ToggleCompletion/5
        [HttpPost]
        public async Task<IActionResult> ToggleCompletion(int id, [FromQuery] string? date = null)
        {
            DateTime completionDate;

            if (date != null)
            {
                completionDate = DateTime.Parse(date + "T00:00:00Z").ToUniversalTime();
            }
            else
            {
                completionDate = DateTime.UtcNow;
            }

            var result = await _habitService.ToggleHabitCompletionAsync(id, completionDate, GetUserId());

            if (result)
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, error = "No se pudo registrar la completación." });
            }
        }

        // GET: Habits/Stats
        public async Task<IActionResult> Stats()
        {
            var userId = GetUserId();
            var stats = await _habitService.GetHabitStatsAsync(userId);

            // Pasar los datos para los gráficos
            ViewBag.WeeklyTrendData = stats.WeeklyTrendData;

            return View(stats);
        }

        // GET: Habits/GetHabitCompletions/5
        public async Task<IActionResult> GetHabitCompletions(int id, [FromQuery] string? startDate = null, [FromQuery] string? endDate = null)
        {
            DateTime? start = startDate != null ? DateTime.Parse(startDate) : null;
            DateTime? end = endDate != null ? DateTime.Parse(endDate) : null;

            var completions = await _habitService.GetHabitCompletionsAsync(id, GetUserId(), start, end);
            return Json(completions.Select(c => new
            {
                date = c.CompletionDate.ToString("yyyy-MM-dd"),
                completed = c.Completed,
                streak = c.StreakCount
            }));
        }

        // POST: Habits/ToggleFavorite/5
        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            var result = await _habitService.ToggleFavoriteAsync(id, GetUserId());

            if (result)
            {
                return Json(new { success = true, isFavorite = true });
            }
            else
            {
                return Json(new { success = false, error = "No se pudo actualizar el favorito." });
            }
        }

        // GET: Habits/GetFavorites
        [HttpGet]
        public async Task<IActionResult> GetFavorites()
        {
            var favorites = await _habitService.GetFavoriteHabitsAsync(GetUserId());
            return Json(favorites.Select(h => new
            {
                id = h.Id,
                name = h.Name,
                icon = h.Icon,
                color = h.ColorCode,
                streak = h.HabitCompletions?.Where(hc => hc.Completed).Max(hc => hc.StreakCount) ?? 0,
                completedToday = h.HabitCompletions?.Any(hc => hc.Completed && hc.CompletionDate.Date == DateTime.UtcNow.Date) == true
            }));
        }

        // POST: Habits/UpdateFavoriteOrder/5
        [HttpPost]
        public async Task<IActionResult> UpdateFavoriteOrder(int id, [FromBody] int newOrder)
        {
            var result = await _habitService.UpdateFavoriteOrderAsync(id, GetUserId(), newOrder);

            if (result)
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, error = "No se pudo actualizar el orden." });
            }
        }

        // GET: Habits/Stats/5
        public async Task<IActionResult> HabitStats(int id)
        {
            var stats = await _habitService.GetHabitDetailStatsAsync(id, GetUserId());
            if (stats == null)
            {
                return NotFound();
            }

            // ✅ NUEVO: Agregar datos de IA
            var userId = GetUserId();
            var userSubscription = await _subscriptionService.GetUserSubscriptionAsync(userId);
            var hasAIFeatures = userSubscription?.Plan?.HasAIFeatures == true;

            if (hasAIFeatures)
            {
                // Obtener recomendaciones específicas para este hábito
                var aiRecommendations = await _habitAIService.GeneratePersonalizedRecommendationsAsync(userId);
                var habitSpecificRecommendations = aiRecommendations
                    .Where(r => r.HabitName == stats.HabitName || r.HabitName == "Todos los hábitos")
                    .Take(2)
                    .ToList();

                ViewBag.AIRecommendations = habitSpecificRecommendations;

                // Obtener predicción de éxito para este hábito
                var successPrediction = await _habitAIService.PredictHabitSuccessAsync(
                    userId, stats.HabitName, stats.Frequency);
                ViewBag.SuccessPrediction = successPrediction;

                // Obtener patrones detectados
                var patterns = await _habitAIService.DetectHabitPatternsAsync(userId);
                ViewBag.AIPatterns = patterns;
            }
            else
            {
                ViewBag.AIRecommendations = new List<HabitRecommendation>();
                ViewBag.SuccessPrediction = null;
                ViewBag.AIPatterns = new List<HabitPattern>();
            }

            ViewBag.HasAIFeatures = hasAIFeatures;
            return View(stats);
        }

        // ✅ NUEVO: Endpoint para predicción de éxito de hábitos
        [HttpPost]
        public async Task<IActionResult> PredictHabitSuccess([FromBody] PredictHabitRequest request)
        {
            try
            {
                var userId = GetUserId();
                var prediction = await _habitAIService.PredictHabitSuccessAsync(userId, request.HabitName, request.Frequency);
                return Json(prediction);
            }
            catch (Exception ex)
            {
                // Fallback si hay error
                return Json(new HabitSuccessPrediction
                {
                    SuccessProbability = 65.0f,
                    ConfidenceLevel = "Medium",
                    KeyFactors = new List<string> { "Predicción básica", "Frecuencia: " + request.Frequency + " días/semana" },
                    Recommendation = "Comienza con consistencia y ajusta según tu progreso"
                });
            }
        }

        // ✅ NUEVO: Endpoint para verificar límites del plan
        [HttpGet]
        public async Task<IActionResult> CheckPlanLimit()
        {
            var subscription = await _subscriptionService.GetUserSubscriptionAsync(GetUserId());
            if (subscription?.Plan == null)
            {
                return Json(new
                {
                    canCreate = false,
                    message = "No tienes un plan activo. Por favor, selecciona un plan.",
                    current = 0,
                    max = 0
                });
            }

            var habits = await _habitService.GetUserHabitsAsync(GetUserId());
            var currentHabitsCount = habits.Count;
            var canCreate = currentHabitsCount < subscription.Plan.MaxHabits;

            var message = canCreate
                ? $"Puedes crear {subscription.Plan.MaxHabits - currentHabitsCount} hábitos más"
                : $"Has alcanzado el límite de {subscription.Plan.MaxHabits} hábitos";

            return Json(new
            {
                canCreate,
                message,
                current = currentHabitsCount,
                max = subscription.Plan.MaxHabits
            });
        }
    }

    // Clase para el request de predicción
    public class PredictHabitRequest
    {
        public string HabitName { get; set; } = string.Empty;
        public int Frequency { get; set; }
    }
}