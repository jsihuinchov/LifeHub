using LifeHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Threading.Tasks;

namespace LifeHub.Controllers
{
    [Authorize]
    public class PlansController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<PlansController> _logger;
        private readonly IDistributedCache _cache;

        public PlansController(
            ISubscriptionService subscriptionService,
            UserManager<IdentityUser> userManager,
            ILogger<PlansController> logger,
            IDistributedCache cache)
        {
            _subscriptionService = subscriptionService;
            _userManager = userManager;
            _logger = logger;
            _cache = cache;
        }

        public async Task<IActionResult> Index()
        {
            var plans = await _subscriptionService.GetActivePlansAsync();
            return View(plans);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePlan(int planId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var newPlan = await _subscriptionService.GetPlanByIdAsync(planId);
                if (newPlan == null)
                {
                    TempData["ErrorMessage"] = "Plan no válido.";
                    return RedirectToAction("Index");
                }

                // Cambiar el plan del usuario
                await _subscriptionService.AssignPlanToUserAsync(user.Id, planId);

                // ✅ LIMPIAR CACHE PARA ACTUALIZACIÓN INMEDIATA
                await _cache.RemoveAsync("subscription:plans:active");
                await _cache.RemoveAsync($"subscription:user:{user.Id}");

                string message;
                var currentSubscription = await _subscriptionService.GetUserSubscriptionAsync(user.Id);
                var currentPlan = currentSubscription?.Plan;

                if (currentPlan != null)
                {
                    message = $"¡Plan cambiado exitosamente de {currentPlan.Name} a {newPlan.Name}!";
                }
                else
                {
                    message = $"¡Plan activado exitosamente: {newPlan.Name}!";
                }

                TempData["SuccessMessage"] = message;
                _logger.LogInformation("Usuario {UserId} cambió al plan {PlanId}", user.Id, planId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar plan del usuario");
                TempData["ErrorMessage"] = "Error al cambiar de plan. Por favor, intenta nuevamente.";
            }

            return RedirectToAction("Index", "Dashboard");
        }

        // Nueva acción para ver detalles del plan
        public async Task<IActionResult> Details(int id)
        {
            var plan = await _subscriptionService.GetPlanByIdAsync(id);
            if (plan == null)
            {
                return NotFound();
            }
            return View(plan);
        }

        // Acción temporal para página de pago
        [HttpGet]
        public async Task<IActionResult> Payment(int planId)
        {
            var plan = await _subscriptionService.GetPlanByIdAsync(planId);
            if (plan == null)
            {
                return NotFound();
            }
            return View(plan);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int planId)
        {
            // Misma lógica que ChangePlan
            return await ChangePlan(planId);
        }
    }
}