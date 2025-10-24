using Google.Apis.Auth;
using LifeHub.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace LifeHub.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<GoogleAuthService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public GoogleAuthService(
            UserManager<IdentityUser> userManager,
            ILogger<GoogleAuthService> logger,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _userManager = userManager;
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        public async Task<GoogleUserInfo> ValidateGoogleTokenAsync(string idToken)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
                
                if (payload.Audience.ToString() != _configuration["Authentication:Google:ClientId"])
                {
                    throw new Exception("Token audience no válido");
                }

                if (payload.ExpirationTimeSeconds < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    throw new Exception("Token expirado");
                }

                return new GoogleUserInfo
                {
                    Email = payload.Email,
                    Name = payload.Name,
                    Picture = payload.Picture,
                    GivenName = payload.GivenName,
                    FamilyName = payload.FamilyName,
                    Sub = payload.Subject
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando token de Google");
                throw;
            }
        }

        public async Task<IdentityUser> FindOrCreateUserAsync(GoogleUserInfo userInfo)
        {
            var user = await _userManager.FindByEmailAsync(userInfo.Email);

            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = userInfo.Email,
                    Email = userInfo.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user);
                
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Error creando usuario: {errors}");
                }

                _logger.LogInformation("Nuevo usuario creado via Google: {Email}", userInfo.Email);

                // Asignar rol
                var isFirstUser = _userManager.Users.Count() == 1;
                if (isFirstUser)
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, "User");
                }

                // Asignar plan gratis
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                    await subscriptionService.AssignPlanToUserAsync(user.Id, 1);
                    _logger.LogInformation("Plan gratis asignado al usuario Google {UserId}", user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error asignando plan al usuario Google {UserId}", user.Id);
                }
            }

            return user;
        }
    }
}