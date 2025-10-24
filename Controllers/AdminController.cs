using LifeHub.Models.ViewModels;
using LifeHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace LifeHub.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<AdminController> _logger;
        private readonly IDistributedCache _cache;

        public AdminController(
            IAdminService adminService,
            UserManager<IdentityUser> userManager,
            ILogger<AdminController> logger,
            IDistributedCache cache)
        {
            _adminService = adminService;
            _userManager = userManager;
            _logger = logger;
            _cache = cache;
        }

        // GET: /Admin
        public async Task<IActionResult> Index()
        {
            try
            {
                var metrics = await _adminService.GetDashboardMetricsAsync();
                return View(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar dashboard admin");
                TempData["ErrorMessage"] = "Error al cargar el dashboard de administración.";
                return View(new AdminDashboardViewModel());
            }
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Users()
        {
            try
            {
                var cacheKey = "admin:users:list";
                List<AdminUserViewModel> users;

                // Cache por 2 minutos para lista de usuarios
                var cachedUsers = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedUsers))
                {
                    _logger.LogInformation("✅ Cache HIT - Lista de usuarios cargada desde Redis");
                    users = JsonSerializer.Deserialize<List<AdminUserViewModel>>(cachedUsers);
                }
                else
                {
                    _logger.LogInformation("🔄 Cache MISS - Cargando lista de usuarios desde BD");
                    users = await _adminService.GetAllUsersAsync();

                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
                    };
                    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(users), cacheOptions);
                }

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar lista de usuarios");
                TempData["ErrorMessage"] = "Error al cargar la lista de usuarios.";
                return View(new List<AdminUserViewModel>());
            }
        }

        // GET: /Admin/UserDetails/{id}
        public async Task<IActionResult> UserDetails(string id)
        {
            try
            {
                var user = await _adminService.GetUserByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado.";
                    return RedirectToAction(nameof(Users));
                }

                var plans = await _adminService.GetAllPlansAsync();
                var model = new ChangePlanViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    AvailablePlans = plans
                };

                ViewBag.User = user;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalles del usuario {UserId}", id);
                TempData["ErrorMessage"] = "Error al cargar los detalles del usuario.";
                return RedirectToAction(nameof(Users));
            }
        }

        // POST: /Admin/BanUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BanUser(string userId, string reason)
        {
            try
            {
                var result = await _adminService.BanUserAsync(userId, reason);
                if (result)
                {
                    TempData["SuccessMessage"] = "Usuario baneado exitosamente.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al banear al usuario.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al banear usuario {UserId}", userId);
                TempData["ErrorMessage"] = "Error al banear al usuario.";
            }

            return RedirectToAction(nameof(Users));
        }

        // POST: /Admin/UnbanUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnbanUser(string userId)
        {
            try
            {
                var result = await _adminService.UnbanUserAsync(userId);
                if (result)
                {
                    TempData["SuccessMessage"] = "Usuario desbaneado exitosamente.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al desbanear al usuario.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desbanear usuario {UserId}", userId);
                TempData["ErrorMessage"] = "Error al desbanear al usuario.";
            }

            return RedirectToAction(nameof(Users));
        }

        // POST: /Admin/ChangeUserPlan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserPlan(ChangePlanViewModel model)
        {
            try
            {
                var result = await _adminService.ChangeUserPlanAsync(model.UserId, model.PlanId);
                if (result)
                {
                    TempData["SuccessMessage"] = "Plan de usuario actualizado exitosamente.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al cambiar el plan del usuario.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar plan del usuario {UserId}", model.UserId);
                TempData["ErrorMessage"] = "Error al cambiar el plan del usuario.";
            }

            return RedirectToAction(nameof(UserDetails), new { id = model.UserId });
        }

        // POST: /Admin/ToggleAdminRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdminRole(string userId)
        {
            try
            {
                var result = await _adminService.ToggleUserRoleAsync(userId, "Admin");
                if (result)
                {
                    TempData["SuccessMessage"] = "Rol de administrador actualizado exitosamente.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al actualizar el rol de administrador.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar rol admin del usuario {UserId}", userId);
                TempData["ErrorMessage"] = "Error al actualizar el rol de administrador.";
            }

            return RedirectToAction(nameof(Users));
        }

        // POST: /Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                // No permitir eliminar al admin principal
                if (userId == "admin-id-123")
                {
                    TempData["ErrorMessage"] = "No se puede eliminar el usuario administrador principal.";
                    return RedirectToAction(nameof(Users));
                }

                var result = await _adminService.DeleteUserAsync(userId);
                if (result)
                {
                    TempData["SuccessMessage"] = "Usuario eliminado exitosamente.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al eliminar al usuario.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario {UserId}", userId);
                TempData["ErrorMessage"] = "Error al eliminar al usuario.";
            }

            return RedirectToAction(nameof(Users));
        }

        // GET: /Admin/Stats
        public async Task<IActionResult> Stats()
        {
            try
            {
                var cacheKey = "admin:stats:usage";
                UsageStatsViewModel usageStats;

                // Cache para estadísticas de uso (5 minutos)
                var cachedUsageStats = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedUsageStats))
                {
                    _logger.LogInformation("✅ Cache HIT - Stats de uso cargados desde Redis");
                    usageStats = JsonSerializer.Deserialize<UsageStatsViewModel>(cachedUsageStats);
                }
                else
                {
                    _logger.LogInformation("🔄 Cache MISS - Cargando stats de uso desde BD");
                    usageStats = await _adminService.GetUsageStatsAsync();

                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    };
                    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(usageStats), cacheOptions);
                }

                // Plan distribution también puede cachearse
                var planCacheKey = "admin:stats:plan_distribution";
                PlanDistributionViewModel planDistribution;

                var cachedPlanDistribution = await _cache.GetStringAsync(planCacheKey);
                if (!string.IsNullOrEmpty(cachedPlanDistribution))
                {
                    planDistribution = JsonSerializer.Deserialize<PlanDistributionViewModel>(cachedPlanDistribution);
                }
                else
                {
                    planDistribution = await _adminService.GetPlanDistributionAsync();
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) // Más tiempo para datos de planes
                    };
                    await _cache.SetStringAsync(planCacheKey, JsonSerializer.Serialize(planDistribution), cacheOptions);
                }

                // Growth stats se mantienen en tiempo real (no cache)
                var growthStats = await _adminService.GetGrowthStatsAsync(30);

                ViewBag.UsageStats = usageStats;
                ViewBag.GrowthStats = growthStats;
                ViewBag.PlanDistribution = planDistribution;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar estadísticas");
                TempData["ErrorMessage"] = "Error al cargar las estadísticas.";
                return View();
            }
        }

        // GET: /Admin/GetGrowthData
        public async Task<IActionResult> GetGrowthData(int days = 30)
        {
            try
            {
                var growthStats = await _adminService.GetGrowthStatsAsync(days);
                return Json(growthStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos de crecimiento");
                return Json(new { error = "Error al obtener datos" });
            }
        }
    }
}