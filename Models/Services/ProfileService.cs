using LifeHub.Models.Entities;
using LifeHub.Data;
using LifeHub.Models.ViewModels.AccountViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; // ← AGREGAR ESTE USING

namespace LifeHub.Services
{
    public class ProfileService : IProfileService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProfileService(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<UserProfile?> GetUserProfileAsync(string userId)
        {
            return await _context.Set<UserProfile>()
                .FirstOrDefaultAsync(up => up.UserId == userId);
        }

        public async Task<UserProfile> CreateOrUpdateUserProfileAsync(string userId, Action<UserProfile> updateAction)
        {
            var profile = await GetUserProfileAsync(userId);
            
            if (profile == null)
            {
                profile = new UserProfile { UserId = userId };
                _context.Set<UserProfile>().Add(profile);
            }

            updateAction(profile);
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return profile;
        }

        public async Task<bool> UpdateUserProfileAsync(string userId, EditProfileViewModel model)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;

                user.Email = model.Email;
                user.UserName = model.UserName ?? model.Email;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded) return false;

                await CreateOrUpdateUserProfileAsync(userId, profile =>
                {
                    profile.Bio = model.Bio;
                    profile.Location = model.Location;
                    profile.WebsiteUrl = model.WebsiteUrl;
                    profile.EmailNotifications = model.EmailNotifications;
                    profile.PushNotifications = model.PushNotifications;
                });

                return true;
            }
            catch (Exception ex)
            {
                // Log error - ahora sí usamos la variable ex
                Console.WriteLine($"Error updating user profile: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
            return result.Succeeded;
        }
    }
}