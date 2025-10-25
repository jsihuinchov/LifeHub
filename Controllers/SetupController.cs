using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LifeHub.Data;
using LifeHub.Services;

namespace LifeHub.Controllers
{
    public class SetupController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<SetupController> _logger;

        public SetupController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ISubscriptionService subscriptionService,
            ILogger<SetupController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> CreateAdmin()
        {
            try
            {
                // Crear roles si no existen
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                    _logger.LogInformation("Rol Admin creado");
                }
                if (!await _roleManager.RoleExistsAsync("User"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("User"));
                    _logger.LogInformation("Rol User creado");
                }

                // Verificar si ya existe el admin
                var adminUser = await _userManager.FindByEmailAsync("admin@lifehub.com");
                if (adminUser != null)
                {
                    // Si existe, asegurarnos de que tenga el rol Admin
                    var isAdmin = await _userManager.IsInRoleAsync(adminUser, "Admin");
                    if (!isAdmin)
                    {
                        await _userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                    return Content("El usuario admin ya existe. Email: admin@lifehub.com<br>" +
                                 "<a href='/Account/Login'>Ir al Login</a>");
                }

                // Crear usuario admin
                var user = new IdentityUser 
                { 
                    UserName = "admin@lifehub.com", 
                    Email = "admin@lifehub.com",
                    EmailConfirmed = true
                };
                
                var result = await _userManager.CreateAsync(user, "Admin123!");
                
                if (result.Succeeded)
                {
                    // Asignar rol de admin
                    await _userManager.AddToRoleAsync(user, "Admin");
                    
                    // Asignar plan premium (ID 9 - Vida Máxima)
                    await _subscriptionService.AssignPlanToUserAsync(user.Id, 9);
                    
                    _logger.LogInformation("Usuario admin creado exitosamente");
                    return Content("✅ <h3>Usuario admin creado exitosamente!</h3>" +
                                 "<p><strong>Email:</strong> admin@lifehub.com</p>" +
                                 "<p><strong>Password:</strong> Admin123!</p>" +
                                 "<p><a href='/Account/Login' class='btn btn-primary'>Ir al Login</a></p>");
                }
                else
                {
                    return Content($"❌ Error al crear admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario admin");
                return Content($"❌ Error: {ex.Message}");
            }
        }
    }
}