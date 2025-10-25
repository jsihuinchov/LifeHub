using LifeHub.Models.ViewModels;
using LifeHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;
using Stripe;
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
        private readonly IStripeService _stripeService;
        private readonly IConfiguration _configuration;

        public PlansController(
            ISubscriptionService subscriptionService,
            UserManager<IdentityUser> userManager,
            ILogger<PlansController> logger,
            IDistributedCache cache,
            IStripeService stripeService,
            IConfiguration configuration)
        {
            _subscriptionService = subscriptionService;
            _userManager = userManager;
            _logger = logger;
            _cache = cache;
            _stripeService = stripeService;
            _configuration = configuration;
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

                // ✅ FORZAR LIMPIEZA DE CACHE ANTES del cambio
                await _cache.RemoveAsync("subscription:plans:active");
                await _cache.RemoveAsync($"subscription:user:{user.Id}");

                // Cambiar el plan del usuario
                await _subscriptionService.AssignPlanToUserAsync(user.Id, planId);

                // ✅ LIMPIAR CACHE DESPUÉS también
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

        [HttpGet]
        public async Task<IActionResult> Payment(int planId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var plan = await _subscriptionService.GetPlanByIdAsync(planId);
            if (plan == null)
            {
                return NotFound();
            }

            // Si es plan gratis, redirigir directamente
            if (plan.Price == 0)
            {
                return RedirectToAction("ChangePlan", new { planId });
            }

            // Crear PaymentIntent para Stripe
            var amount = (long)(plan.Price * 100);
            var paymentIntent = await _stripeService.CreatePaymentIntentAsync(amount, "usd", user.Email);

            // ✅ SOLO USAR TEMPDATA (más simple)
            TempData["LastPaymentIntentId"] = paymentIntent.Id;
            TempData["CurrentPlanId"] = planId.ToString();

            _logger.LogInformation("💰 PaymentIntent creado: {PaymentIntentId} para plan {PlanId}", paymentIntent.Id, planId);

            var model = new PaymentViewModel
            {
                Plan = plan,
                PaymentIntentClientSecret = paymentIntent.ClientSecret,
                StripePublishableKey = _configuration["StripeSettings:PublishableKey"]
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int planId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Obtener el PaymentIntentId de TempData
                var paymentIntentId = TempData["LastPaymentIntentId"] as string;

                if (string.IsNullOrEmpty(paymentIntentId))
                {
                    _logger.LogWarning("No se pudo obtener el PaymentIntentId para el plan {PlanId}", planId);
                    TempData["ErrorMessage"] = "No se pudo identificar el pago. Por favor, contacta con soporte.";
                    return RedirectToAction("Payment", new { planId });
                }

                // Verificar el estado del pago
                var paymentIntent = await _stripeService.GetPaymentIntentAsync(paymentIntentId);

                if (paymentIntent.Status == "succeeded")
                {
                    // Pago exitoso, asignar el plan al usuario
                    await _subscriptionService.AssignPlanToUserAsync(user.Id, planId);

                    // ✅ LIMPIAR CACHE MÁS AGRESIVAMENTE
                    await _cache.RemoveAsync("subscription:plans:active");
                    await _cache.RemoveAsync($"subscription:user:{user.Id}");

                    // ✅ FORZAR ACTUALIZACIÓN
                    TempData["ForceRefresh"] = "true";
                    TempData["NewPlanId"] = planId.ToString();

                    _logger.LogInformation("Pago exitoso - Usuario {UserId} cambió al plan {PlanId}", user.Id, planId);

                    TempData["SuccessMessage"] = "¡Pago procesado exitosamente! Tu plan ha sido activado.";
                    return RedirectToAction("PaymentSuccess", new { planId, paymentIntentId });
                }
                else
                {
                    _logger.LogWarning("Pago no exitoso - Estado: {Status}", paymentIntent.Status);
                    TempData["ErrorMessage"] = $"El pago no se completó correctamente. Estado: {paymentIntent.Status}";
                    return RedirectToAction("Payment", new { planId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar pago para plan {PlanId}", planId);
                TempData["ErrorMessage"] = "Error al procesar el pago. Por favor, intenta nuevamente.";
                return RedirectToAction("Payment", new { planId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(int planId, string paymentIntentId = null)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                _logger.LogInformation("🔄 PaymentSuccess iniciado - User: {UserId}, Plan: {PlanId}", user.Id, planId);

                var plan = await _subscriptionService.GetPlanByIdAsync(planId);
                if (plan == null)
                {
                    return NotFound();
                }

                // ✅ OBTENER PAYMENT INTENT ID DE TEMPDATA
                if (string.IsNullOrEmpty(paymentIntentId))
                {
                    paymentIntentId = TempData["LastPaymentIntentId"] as string;
                    _logger.LogInformation("📝 Usando PaymentIntentId de TempData: {PaymentIntentId}", paymentIntentId);
                }

                PaymentIntent paymentIntent = null;
                bool paymentVerified = false;

                // ✅ VERIFICAR EL PAGO Y ASIGNAR PLAN
                if (!string.IsNullOrEmpty(paymentIntentId))
                {
                    try
                    {
                        paymentIntent = await _stripeService.GetPaymentIntentAsync(paymentIntentId);
                        _logger.LogInformation("💰 Estado del pago: {Status}", paymentIntent.Status);

                        if (paymentIntent.Status == "succeeded")
                        {
                            paymentVerified = true;
                            _logger.LogInformation("🎉 Pago verificado con Stripe");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error verificando pago con Stripe");
                    }
                }

                // ✅ ASIGNAR PLAN AL USUARIO (SIEMPRE)
                _logger.LogInformation("🔄 Asignando plan {PlanId} al usuario {UserId}", planId, user.Id);

                await _subscriptionService.AssignPlanToUserAsync(user.Id, planId);

                // Limpiar cache
                await _cache.RemoveAsync("subscription:plans:active");
                await _cache.RemoveAsync($"subscription:user:{user.Id}");

                _logger.LogInformation("✅ Plan {PlanName} asignado exitosamente", plan.Name);

                ViewBag.PlanName = plan.Name;
                ViewBag.PlanPrice = plan.FormattedPrice;

                if (paymentIntent != null)
                {
                    ViewBag.PaymentAmount = $"${paymentIntent.Amount / 100.0:F2}";
                    ViewBag.PaymentStatus = paymentIntent.Status;
                }
                else
                {
                    ViewBag.PaymentAmount = plan.FormattedPrice;
                    ViewBag.PaymentStatus = "succeeded";
                }

                ViewBag.PaymentDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                ViewBag.PaymentVerified = paymentVerified;

                _logger.LogInformation("✅ PaymentSuccess completado");

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error crítico en PaymentSuccess");
                TempData["ErrorMessage"] = "Error al procesar el pago. Por favor, contacta con soporte.";
                return RedirectToAction("Index", "Plans");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DeepDebug()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Content("No user");

            var result = $"<h3>🔍 DEEP DEBUG - User: {user.Email}</h3>";

            // 1. Verificar el usuario
            result += $"<h4>1. Información del Usuario:</h4>";
            result += $"<p>ID: {user.Id}</p>";
            result += $"<p>Email: {user.Email}</p>";

            // 2. Verificar planes disponibles
            result += $"<h4>2. Planes Disponibles:</h4>";
            var plans = await _subscriptionService.GetActivePlansAsync();
            foreach (var plan in plans)
            {
                result += $"<p>Plan ID: {plan.Id}, Nombre: {plan.Name}, Precio: {plan.Price}</p>";
            }

            // 3. Verificar suscripción actual
            result += $"<h4>3. Suscripción Actual (desde servicio):</h4>";
            var subscription = await _subscriptionService.GetUserSubscriptionAsync(user.Id);
            result += $"<p>Plan: {subscription?.Plan?.Name ?? "NULL"}</p>";
            result += $"<p>Plan ID: {subscription?.PlanId ?? 0}</p>";
            result += $"<p>Activa: {subscription?.IsActive ?? false}</p>";

            // 4. Verificar DIRECTAMENTE en la base de datos
            result += $"<h4>4. Verificación Directa en BD:</h4>";
            try
            {
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await connection.OpenAsync();

                    // Ver todas las suscripciones
                    var cmd = new NpgsqlCommand(@"
                SELECT us.id, us.plan_id, p.name, us.is_active, us.created_at 
                FROM user_subscriptions us 
                LEFT JOIN subscription_plans p ON us.plan_id = p.id 
                WHERE us.user_id = @userId 
                ORDER BY us.created_at DESC", connection);
                    cmd.Parameters.AddWithValue("userId", user.Id);

                    var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        result += $"<p>Subscription ID: {reader.GetInt32(0)}, Plan ID: {reader.GetInt32(1)}, Plan: {reader.GetString(2)}, Active: {reader.GetBoolean(3)}, Created: {reader.GetDateTime(4)}</p>";
                    }
                    reader.Close();

                    // Verificar si hay algún error de constraints
                    result += $"<h4>5. Verificación de Planes en BD:</h4>";
                    var cmd2 = new NpgsqlCommand("SELECT id, name FROM subscription_plans ORDER BY id", connection);
                    var reader2 = await cmd2.ExecuteReaderAsync();
                    while (await reader2.ReadAsync())
                    {
                        result += $"<p>Plan ID: {reader2.GetInt32(0)}, Name: {reader2.GetString(1)}</p>";
                    }
                }
            }
            catch (Exception ex)
            {
                result += $"<p style='color: red;'>Error en BD: {ex.Message}</p>";
            }

            // 6. Probar cambio de plan manualmente
            result += $"<h4>6. Probar Cambio Manual:</h4>";
            result += @"<form method='post' action='/Plans/TestChangePlan'>
        <input type='number' name='planId' value='2' />
        <input type='submit' value='Probar Cambiar a Plan 2' />
    </form>";

            return Content(result, "text/html");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> TestChangePlan(int planId)
        {
            var user = await _userManager.GetUserAsync(User);
            var result = $"<h3>Test Change Plan - Plan ID: {planId}</h3>";

            try
            {
                _logger.LogInformation("🧪 TEST - Cambiando usuario {UserId} al plan {PlanId}", user.Id, planId);

                // Limpiar cache
                await _cache.RemoveAsync($"subscription:user:{user.Id}");

                // Llamar directamente al método del servicio
                await _subscriptionService.AssignPlanToUserAsync(user.Id, planId);

                // Verificar resultado
                var newSub = await _subscriptionService.GetUserSubscriptionAsync(user.Id);

                result += $"<p>✅ Operación completada</p>";
                result += $"<p>Nuevo plan: {newSub?.Plan?.Name ?? "NULL"}</p>";
                result += $"<p><a href='/Plans/DeepDebug'>Volver a Debug</a></p>";
            }
            catch (Exception ex)
            {
                result += $"<p style='color: red;'>❌ ERROR: {ex.Message}</p>";
                result += $"<p>Stack: {ex.StackTrace}</p>";
            }

            return Content(result, "text/html");
        }
    }
}