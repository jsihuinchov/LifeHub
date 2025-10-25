using System.Collections.Generic;
using System.Threading.Tasks;

namespace LifeHub.Services
{
    public interface ISubscriptionService
    {
        Task<List<LifeHub.Models.SubscriptionPlan>> GetActivePlansAsync();
        Task<LifeHub.Models.SubscriptionPlan?> GetPlanByIdAsync(int id);
        
        // Métodos existentes
        Task AssignPlanToUserAsync(string userId, int planId);
        Task<LifeHub.Models.UserSubscription?> GetUserSubscriptionAsync(string userId);
        Task<bool> UserHasFeatureAccessAsync(string userId, string feature);
        Task<bool> CanUserCreateHabitAsync(string userId);
        Task<bool> CanUserCreateTransactionAsync(string userId);
        
        // ✅ NUEVOS MÉTODOS PARA LÍMITES DINÁMICOS
        Task<bool> CanUserCreateBudgetAsync(string userId);
        Task<(int current, int max)> GetTransactionUsageAsync(string userId);
        Task<(int current, int max)> GetBudgetUsageAsync(string userId);
    }
}