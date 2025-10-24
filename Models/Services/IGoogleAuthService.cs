using LifeHub.Models.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace LifeHub.Services
{
    public interface IGoogleAuthService
    {
        Task<GoogleUserInfo> ValidateGoogleTokenAsync(string idToken);
        Task<IdentityUser> FindOrCreateUserAsync(GoogleUserInfo userInfo);
    }
}