using LifeHub.Models.Entities;
using LifeHub.Models.ViewModels.AccountViewModels;

namespace LifeHub.Services
{
    public interface IProfileService
    {
        Task<UserProfile?> GetUserProfileAsync(string userId);
        Task<UserProfile> CreateOrUpdateUserProfileAsync(string userId, Action<UserProfile> updateAction);
        Task<bool> UpdateUserProfileAsync(string userId, EditProfileViewModel model);
        Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword);
    }
}