using LifeHub.Models.Entities;
using LifeHub.Models.ViewModels;

namespace LifeHub.Services
{
    public interface IHabitService
    {
        Task<List<Habit>> GetUserHabitsAsync(string userId);
        Task<List<Habit>> GetFavoriteHabitsAsync(string userId); // ✅ NUEVO
        Task<Habit?> GetHabitByIdAsync(int id, string userId);
        Task<bool> CreateHabitAsync(Habit habit, string userId);
        Task<bool> UpdateHabitAsync(Habit habit, string userId);
        Task<bool> DeleteHabitAsync(int id, string userId);
        Task<bool> ToggleHabitCompletionAsync(int habitId, DateTime date, string userId);
        Task<bool> ToggleFavoriteAsync(int habitId, string userId); // ✅ NUEVO
        Task<bool> UpdateFavoriteOrderAsync(int habitId, string userId, int newOrder); // ✅ NUEVO
        Task<HabitStatsViewModel> GetHabitStatsAsync(string userId);
        Task<List<HabitCompletion>> GetHabitCompletionsAsync(int habitId, string userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<int> GetCurrentStreakAsync(int habitId, string userId);
        Task<bool> CanUserCreateMoreHabitsAsync(string userId);
        Task<int> GetUserHabitLimitAsync(string userId);
        Task<(int current, int max)> GetHabitUsageAsync(string userId);
        Task<HabitDetailStatsViewModel> GetHabitDetailStatsAsync(int habitId, string userId); // ✅ NUEVO
    }
}