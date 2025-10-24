using LifeHub.Models;
using LifeHub.Models.ViewModels;

namespace LifeHub.Services
{
    public interface IAdminService
    {
        // Dashboard
        Task<AdminDashboardViewModel> GetDashboardMetricsAsync();
        Task<List<UserStatsViewModel>> GetUserStatsAsync();
        Task<PlanDistributionViewModel> GetPlanDistributionAsync();
        
        // Gestión de Usuarios
        Task<List<AdminUserViewModel>> GetAllUsersAsync();
        Task<AdminUserViewModel?> GetUserByIdAsync(string userId);
        Task<bool> BanUserAsync(string userId, string reason, bool permanent = true, int? days = null);
        Task<bool> UnbanUserAsync(string userId);
        Task<bool> ChangeUserPlanAsync(string userId, int planId);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> ToggleUserRoleAsync(string userId, string role);
        
        // Estadísticas
        Task<UsageStatsViewModel> GetUsageStatsAsync();
        Task<List<GrowthStatsViewModel>> GetGrowthStatsAsync(int days = 30);
        
        // Planes
        Task<List<SubscriptionPlan>> GetAllPlansAsync();
    }
}