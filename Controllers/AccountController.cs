using LifeHub.Models.ViewModels;
using LifeHub.Models.ViewModels.AccountViewModels;
using LifeHub.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using LifeHub.Models;
using LifeHub.Data;
using Microsoft.EntityFrameworkCore;


namespace LifeHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<AccountController> _logger;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IProfileService _profileService;
        private readonly ApplicationDbContext _context; // Agregar esta línea

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ISubscriptionService subscriptionService,
            ILogger<AccountController> logger,
            IGoogleAuthService googleAuthService,
            IProfileService profileService,
            ApplicationDbContext context) // Agregar este parámetro
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _subscriptionService = subscriptionService;
            _logger = logger;
            _googleAuthService = googleAuthService;
            _profileService = profileService;
            _context = context; // Agregar esta línea
        }

        // GET: /Account/Register
        [HttpGet]
        public async Task<IActionResult> Register(int planId = 1)
        {
            try
            {
                var plan = await _subscriptionService.GetPlanByIdAsync(planId);
                var model = new RegisterViewModel
                {
                    SelectedPlanId = planId,
                    SelectedPlanName = plan?.Name ?? "Plan Gratis"
                };

                // Si no viene de un plan específico, mostrar advertencia
                if (planId == 1)
                {
                    ViewBag.ShowPlanWarning = true;
                }

                return View(model);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al cargar página de registro con plan {PlanId}", planId);
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuario creado exitosamente: {Email}", model.Email);

                    // ✅ Si es el primer usuario, hacerlo admin
                    var isFirstUser = _userManager.Users.Count() == 1;
                    if (isFirstUser)
                    {
                        await _userManager.AddToRoleAsync(user, "Admin");
                        _logger.LogInformation("Primer usuario {Email} asignado como Admin", model.Email);
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, "User");
                    }

                    try
                    {
                        // Asignar el plan seleccionado al usuario
                        await _subscriptionService.AssignPlanToUserAsync(user.Id, model.SelectedPlanId);
                        _logger.LogInformation("Plan {PlanId} asignado al usuario {UserId}", model.SelectedPlanId, user.Id);
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex, "Error al asignar plan al usuario {UserId}", user.Id);
                        // Continuamos aunque falle la asignación del plan
                    }

                    // Iniciar sesión automáticamente
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    var message = isFirstUser
                        ? $"¡Bienvenido a LifeHub! Eres el primer usuario y has sido asignado como Administrador."
                        : $"¡Bienvenido a LifeHub! Tu cuenta con el {model.SelectedPlanName} ha sido activada.";

                    TempData["SuccessMessage"] = message;
                    return RedirectToAction("Index", isFirstUser ? "Admin" : "Dashboard");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Si llegamos aquí, algo falló, redisplay form
            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuario {Email} ha iniciado sesión", model.Email);

                    // ✅ Redirigir admin al panel de administración
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                        if (isAdmin)
                        {
                            _logger.LogInformation("Admin {Email} redirigido al panel de administración", model.Email);
                            return RedirectToAction("Index", "Admin");
                        }
                    }

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Dashboard");
                }
                
                ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
            }

            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Usuario ha cerrado sesión");
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        

        // ✅ NUEVO: Endpoint directo (redirección a Google)
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GoogleLoginDirect(string? returnUrl = null)
        {
            try
            {
                _logger.LogInformation("Iniciando autenticación Google directa");
                
                var redirectUrl = Url.Action("GoogleLoginCallback", "Account", new { ReturnUrl = returnUrl });
                var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
                
                _logger.LogInformation("Redireccionando a Google Authentication");
                return Challenge(properties, "Google");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GoogleLoginDirect");
                TempData["ErrorMessage"] = "Error al iniciar autenticación con Google.";
                return RedirectToAction("Login");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            _logger.LogInformation("=== INICIANDO GOOGLE LOGIN CALLBACK ===");

            if (remoteError != null)
            {
                _logger.LogError("❌ Error externo de Google: {Error}", remoteError);
                TempData["ErrorMessage"] = $"Error de Google: {remoteError}";
                return RedirectToAction("Login");
            }

            _logger.LogInformation("✅ No hay error remoto de Google");

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogError("❌ No se pudo obtener información externa de Google");
                TempData["ErrorMessage"] = "Error cargando información de Google.";
                return RedirectToAction("Login");
            }

            _logger.LogInformation("✅ Información de Google obtenida correctamente");

            var email = info.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = info.Principal.FindFirst(ClaimTypes.Name)?.Value;
            var givenName = info.Principal.FindFirst(ClaimTypes.GivenName)?.Value;

            _logger.LogInformation("📧 Email obtenido: {Email}", email ?? "NULL");

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogError("❌ No se pudo obtener el email de Google");
                TempData["ErrorMessage"] = "No se pudo obtener el email de Google.";
                return RedirectToAction("Login");
            }

            // ✅ BUSCAR USUARIO POR EMAIL
            _logger.LogInformation("🔍 Buscando usuario por email: {Email}", email);
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                _logger.LogInformation("✅ Usuario EXISTENTE encontrado: {Email}", email);

                // Usuario existe, hacer login
                var logins = await _userManager.GetLoginsAsync(user);
                var existingLogin = logins.FirstOrDefault(l => l.LoginProvider == info.LoginProvider && l.ProviderKey == info.ProviderKey);

                if (existingLogin == null)
                {
                    _logger.LogInformation("➕ Agregando login de Google para usuario existente");
                    await _userManager.AddLoginAsync(user, info);
                }

                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation("🎉 Login exitoso para usuario existente: {Email}", email);

                return RedirectToLocal(returnUrl);
            }
            else
            {
                _logger.LogInformation("🆕 Usuario NUEVO detectado: {Email}", email);

                // ✅ CREAR NUEVO USUARIO AUTOMÁTICAMENTE
                try
                {
                    _logger.LogInformation("👤 Creando nuevo usuario para: {Email}", email);

                    user = new IdentityUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true // Google ya verificó el email
                    };

                    var createResult = await _userManager.CreateAsync(user);

                    if (createResult.Succeeded)
                    {
                        _logger.LogInformation("✅ Usuario creado exitosamente: {Email}", email);

                        // Agregar el login de Google
                        await _userManager.AddLoginAsync(user, info);
                        _logger.LogInformation("✅ Login de Google agregado");

                        // Asignar rol
                        var isFirstUser = _userManager.Users.Count() == 1;
                        if (isFirstUser)
                        {
                            await _userManager.AddToRoleAsync(user, "Admin");
                            _logger.LogInformation("👑 Primer usuario asignado como Admin");
                        }
                        else
                        {
                            await _userManager.AddToRoleAsync(user, "User");
                            _logger.LogInformation("👤 Usuario asignado como User");
                        }

                        // Asignar plan gratis por defecto
                        try
                        {
                            await _subscriptionService.AssignPlanToUserAsync(user.Id, 1);
                            _logger.LogInformation("📋 Plan gratis asignado al usuario");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "⚠️ Error asignando plan, pero continuamos...");
                        }

                        // Iniciar sesión
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation("🎉 Usuario NUEVO registrado y logueado: {Email}", email);

                        TempData["SuccessMessage"] = $"¡Bienvenido a LifeHub {givenName ?? name}! Tu cuenta ha sido creada exitosamente.";
                        return RedirectToAction("Index", "Dashboard");
                    }
                    else
                    {
                        var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                        _logger.LogError("❌ Error creando usuario: {Errors}", errors);
                        TempData["ErrorMessage"] = $"Error creando cuenta: {errors}";
                        return RedirectToAction("Login");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error inesperado creando usuario");
                    TempData["ErrorMessage"] = "Error inesperado creando la cuenta. Por favor, intenta de nuevo.";
                    return RedirectToAction("Login");
                }
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> DebugGoogleAuth()
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
            {
                return Content("No hay información de Google disponible");
            }

            var result = $"<h3>Información de Google:</h3>";
            result += $"<p>LoginProvider: {info.LoginProvider}</p>";
            result += $"<p>ProviderKey: {info.ProviderKey}</p>";
            result += $"<p>DisplayName: {info.ProviderDisplayName}</p>";

            result += "<h3>Claims:</h3>";
            foreach (var claim in info.Principal.Claims)
            {
                result += $"<p>{claim.Type}: {claim.Value}</p>";
            }

            return Content(result, "text/html");
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Dashboard");
        }

        // ✅ LOGIN con Google - SOLO para usuarios existentes
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            _logger.LogInformation("🔐 Iniciando LOGIN con Google");

            var redirectUrl = Url.Action("GoogleLoginCallback", "Account", new
            {
                ReturnUrl = returnUrl,
                ActionType = "login" // ← Identificador para login
            });

            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return Challenge(properties, "Google");
        }

        // ✅ REGISTRO con Google - SOLO para usuarios nuevos  
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GoogleRegister(string? returnUrl = null)
        {
            _logger.LogInformation("👤 Iniciando REGISTRO con Google");

            var redirectUrl = Url.Action("GoogleLoginCallback", "Account", new
            {
                ReturnUrl = returnUrl,
                ActionType = "register" // ← Identificador para registro
            });

            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return Challenge(properties, "Google");
        }

        // ✅ MÉTODO PARA LOGIN (solo usuarios existentes)
        private async Task<IActionResult> HandleGoogleLogin(IdentityUser? user, ExternalLoginInfo info, string email, string? returnUrl)
        {
            if (user == null)
            {
                _logger.LogWarning("🚫 LOGIN fallido - Usuario no existe: {Email}", email);
                TempData["ErrorMessage"] = "Este usuario no existe. Por favor, regístrate primero con email y contraseña.";
                return RedirectToAction("Login");
            }

            _logger.LogInformation("✅ LOGIN exitoso - Usuario encontrado: {Email}", email);

            // Agregar login de Google si no existe
            var logins = await _userManager.GetLoginsAsync(user);
            if (!logins.Any(l => l.LoginProvider == info.LoginProvider && l.ProviderKey == info.ProviderKey))
            {
                await _userManager.AddLoginAsync(user, info);
                _logger.LogInformation("➕ Login de Google agregado para usuario existente");
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("🎉 Usuario logueado exitosamente: {Email}", email);

            return RedirectToLocal(returnUrl);
        }

        // ✅ MÉTODO PARA REGISTRO (solo usuarios nuevos)
        private async Task<IActionResult> HandleGoogleRegister(IdentityUser? user, ExternalLoginInfo info, string email, string? name, string? givenName, string? returnUrl)
        {
            if (user != null)
            {
                _logger.LogWarning("🚫 REGISTRO fallido - Usuario ya existe: {Email}", email);
                TempData["ErrorMessage"] = "Este usuario ya existe. Por favor, inicia sesión en lugar de registrarte.";
                return RedirectToAction("Login");
            }

            _logger.LogInformation("🆕 REGISTRO - Creando nuevo usuario: {Email}", email);

            try
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(user);

                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    _logger.LogError("❌ Error creando usuario: {Errors}", errors);
                    TempData["ErrorMessage"] = $"Error creando cuenta: {errors}";
                    return RedirectToAction("Register");
                }

                _logger.LogInformation("✅ Usuario creado exitosamente: {Email}", email);

                // Agregar login de Google
                await _userManager.AddLoginAsync(user, info);

                // Asignar rol
                var isFirstUser = _userManager.Users.Count() == 1;
                await _userManager.AddToRoleAsync(user, isFirstUser ? "Admin" : "User");

                // Asignar plan gratis
                try
                {
                    await _subscriptionService.AssignPlanToUserAsync(user.Id, 1);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "⚠️ Error asignando plan");
                }

                // Iniciar sesión
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation("🎉 Usuario registrado y logueado: {Email}", email);

                TempData["SuccessMessage"] = $"¡Bienvenido a LifeHub {givenName ?? name}! Tu cuenta ha sido creada exitosamente.";
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inesperado en registro");
                TempData["ErrorMessage"] = "Error inesperado creando la cuenta.";
                return RedirectToAction("Register");
            }
        }

        // MÉTODO HELPER SIMPLIFICADO PARA OBTENER SUSCRIPCIÓN
        private async Task<(string PlanName, DateTime? EndDate)> GetUserSubscriptionInfoAsync(string userId)
        {
            try
            {
                // Intentar obtener la suscripción activa de la base de datos
                var subscription = await _context.UserSubscriptions
                    .Include(us => us.Plan)
                    .Where(us => us.UserId == userId && us.IsActive && us.EndDate > DateTime.UtcNow)
                    .OrderByDescending(us => us.StartDate)
                    .FirstOrDefaultAsync();

                if (subscription != null && subscription.Plan != null)
                {
                    return (subscription.Plan.Name, subscription.EndDate);
                }

                // Si no hay suscripción activa, devolver plan gratis por defecto
                return ("Plan Gratis", DateTime.UtcNow.AddDays(30));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener información de suscripción del usuario {UserId}", userId);
                // En caso de error, devolver plan gratis
                return ("Plan Gratis", DateTime.UtcNow.AddDays(30));
            }
        }

        // VER PERFIL
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                var userProfile = await _profileService.GetUserProfileAsync(user.Id);
                var (currentPlan, subscriptionEndDate) = await GetUserSubscriptionInfoAsync(user.Id);
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

                var model = new LifeHub.Models.ViewModels.AccountViewModels.ProfileViewModel
                {
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName,
                    EmailConfirmed = user.EmailConfirmed,
                    Bio = userProfile?.Bio,
                    Location = userProfile?.Location,
                    WebsiteUrl = userProfile?.WebsiteUrl,
                    EmailNotifications = userProfile?.EmailNotifications ?? true,
                    PushNotifications = userProfile?.PushNotifications ?? true,
                    CurrentPlan = currentPlan,
                    SubscriptionEndDate = subscriptionEndDate,
                    IsAdmin = isAdmin,
                    MemberSince = userProfile?.UpdatedAt ?? DateTime.UtcNow
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el perfil del usuario");
                TempData["ErrorMessage"] = "Error al cargar el perfil.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        // EDITAR PERFIL - GET
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                var userProfile = await _profileService.GetUserProfileAsync(user.Id);

                var model = new LifeHub.Models.ViewModels.AccountViewModels.EditProfileViewModel
                {
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName,
                    Bio = userProfile?.Bio,
                    Location = userProfile?.Location,
                    WebsiteUrl = userProfile?.WebsiteUrl,
                    EmailNotifications = userProfile?.EmailNotifications ?? true,
                    PushNotifications = userProfile?.PushNotifications ?? true
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar edición de perfil");
                TempData["ErrorMessage"] = "Error al cargar el formulario de edición.";
                return RedirectToAction("Profile");
            }
        }

        // EDITAR PERFIL - POST
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(LifeHub.Models.ViewModels.AccountViewModels.EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                var success = await _profileService.UpdateUserProfileAsync(user.Id, model);

                if (success)
                {
                    TempData["SuccessMessage"] = "Perfil actualizado correctamente.";
                    return RedirectToAction("Profile");
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al actualizar el perfil.";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar perfil");
                TempData["ErrorMessage"] = "Error inesperado al actualizar el perfil.";
                return View(model);
            }
        }

        // CAMBIAR CONTRASEÑA - GET
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // CAMBIAR CONTRASEÑA - POST
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(LifeHub.Models.ViewModels.AccountViewModels.ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                var success = await _profileService.ChangePasswordAsync(user.Id, model.OldPassword, model.NewPassword);

                if (success)
                {
                    await _signInManager.SignOutAsync();
                    TempData["SuccessMessage"] = "Contraseña cambiada correctamente. Por favor, inicia sesión nuevamente.";
                    return RedirectToAction("Login");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Error al cambiar la contraseña. Verifica tu contraseña actual.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña");
                ModelState.AddModelError(string.Empty, "Error inesperado al cambiar la contraseña.");
                return View(model);
            }
        }

        // MÉTODO HELPER PARA OBTENER SUSCRIPCIÓN ACTIVA
        private async Task<UserSubscription?> GetUserActiveSubscriptionAsync(string userId)
        {
            try
            {
                return await _context.UserSubscriptions
                    .Include(us => us.Plan)
                    .Where(us => us.UserId == userId && us.IsActive && us.EndDate > DateTime.UtcNow)
                    .OrderByDescending(us => us.StartDate)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener suscripción activa del usuario {UserId}", userId);
                return null;
            }
        }
    }
}