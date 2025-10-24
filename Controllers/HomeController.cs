using LifeHub.Models;
using LifeHub.Models.ViewModels;
using LifeHub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LifeHub.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IEmailService _emailService;
        private readonly IDistributedCache _cache;

        public HomeController(
            ILogger<HomeController> logger, 
            ISubscriptionService subscriptionService, 
            IEmailService emailService,
            IDistributedCache cache)
        {
            _logger = logger;
            _subscriptionService = subscriptionService;
            _emailService = emailService;
            _cache = cache;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                const string cacheKey = "home:plans:active";
                List<SubscriptionPlan> plans;

                // Intentar obtener de Redis
                var cachedPlans = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedPlans))
                {
                    _logger.LogInformation("✅ Cache HIT - Planes cargados desde Redis");
                    plans = JsonSerializer.Deserialize<List<SubscriptionPlan>>(cachedPlans);
                }
                else
                {
                    _logger.LogInformation("🔄 Cache MISS - Cargando planes desde base de datos");
                    
                    // Cargar desde base de datos
                    plans = await _subscriptionService.GetActivePlansAsync();
                    
                    if (plans != null && plans.Any())
                    {
                        // Guardar en Redis por 30 minutos
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                        };
                        
                        var serializedPlans = JsonSerializer.Serialize(plans);
                        await _cache.SetStringAsync(cacheKey, serializedPlans, cacheOptions);
                        
                        _logger.LogInformation("💾 Planes guardados en Redis por 30 minutos");
                    }
                }

                return View(plans ?? new List<SubscriptionPlan>());
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "❌ Error al cargar los planes de suscripción");
                
                // Fallback: cargar sin cache
                try
                {
                    var plans = await _subscriptionService.GetActivePlansAsync();
                    return View(plans ?? new List<SubscriptionPlan>());
                }
                catch
                {
                    return View(new List<SubscriptionPlan>());
                }
            }
        }

        // Nueva acción: mostrar detalles de un plan
        public async Task<IActionResult> Plan(int id)
        {
            try
            {
                var cacheKey = $"home:plan:{id}";
                SubscriptionPlan plan;

                // Intentar obtener de Redis
                var cachedPlan = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedPlan))
                {
                    _logger.LogInformation("✅ Cache HIT - Plan {PlanId} cargado desde Redis", id);
                    plan = JsonSerializer.Deserialize<SubscriptionPlan>(cachedPlan);
                }
                else
                {
                    _logger.LogInformation("🔄 Cache MISS - Cargando plan {PlanId} desde base de datos", id);
                    
                    // Cargar desde base de datos
                    plan = await _subscriptionService.GetPlanByIdAsync(id);
                    
                    if (plan != null)
                    {
                        // Guardar en Redis por 30 minutos
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                        };
                        
                        var serializedPlan = JsonSerializer.Serialize(plan);
                        await _cache.SetStringAsync(cacheKey, serializedPlan, cacheOptions);
                        
                        _logger.LogInformation("💾 Plan {PlanId} guardado en Redis por 30 minutos", id);
                    }
                }

                if (plan == null)
                {
                    return NotFound();
                }
                return View(plan);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "❌ Error al obtener el plan con ID {Id}", id);
                
                // Fallback: cargar sin cache
                try
                {
                    var plan = await _subscriptionService.GetPlanByIdAsync(id);
                    if (plan == null)
                    {
                        return NotFound();
                    }
                    return View(plan);
                }
                catch
                {
                    return RedirectToAction("Index");
                }
            }
        }

        // Acción para limpiar cache manualmente (útil para desarrollo)
        [HttpPost]
        public async Task<IActionResult> ClearCache()
        {
            try
            {
                // Limpiar todas las claves relacionadas con planes
                await RemoveCacheByPattern("home:plans:*");
                await RemoveCacheByPattern("home:plan:*");
                
                TempData["SuccessMessage"] = "✅ Cache limpiado exitosamente";
                _logger.LogInformation("Cache de planes limpiado manualmente");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "❌ Error al limpiar el cache";
                _logger.LogError(ex, "Error al limpiar cache manualmente");
            }
            
            return RedirectToAction("Index");
        }

        // Método helper para limpiar cache por patrón
        private async Task RemoveCacheByPattern(string pattern)
        {
            // En una implementación real, necesitarías acceso al servidor Redis
            // Para esta implementación simple, limpiamos las claves principales
            await _cache.RemoveAsync("home:plans:active");
        }

        // Acción simulada de suscripción (ejemplo académico)
        [HttpPost]
        public async Task<IActionResult> Subscribe(int planId)
        {
            _logger.LogInformation("El usuario se suscribió al plan {PlanId}", planId);

            // Limpiar cache después de una suscripción
            await _cache.RemoveAsync("home:plans:active");
            await _cache.RemoveAsync($"home:plan:{planId}");

            // Aquí podrías guardar en la tabla user_subscriptions
            // o simplemente mostrar una vista de confirmación.

            TempData["Message"] = $"¡Te has suscrito exitosamente al plan con ID {planId}!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendContactEmail(ContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Por favor, completa todos los campos correctamente.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                // Agregar información adicional
                model.IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                model.FechaEnvio = DateTime.Now;

                var result = await _emailService.SendContactEmailAsync(model);

                if (result)
                {
                    TempData["SuccessMessage"] = "¡Mensaje enviado exitosamente! Te contactaremos en menos de 24 horas.";
                    _logger.LogInformation("Formulario de contacto enviado por {Nombre} ({Email})", model.Nombre, model.Email);
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al enviar el mensaje. Por favor, intenta nuevamente.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar formulario de contacto");
                TempData["ErrorMessage"] = "Error interno del sistema. Por favor, intenta más tarde.";
            }

            return RedirectToAction("Index", "Home");
        }

        // Acción para ver estadísticas de cache (útil para desarrollo)
        public async Task<IActionResult> CacheStats()
        {
            var stats = new
            {
                PlansCacheExists = !string.IsNullOrEmpty(await _cache.GetStringAsync("home:plans:active")),
                CacheKey = "home:plans:active",
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return Json(stats);
        }
    }
}