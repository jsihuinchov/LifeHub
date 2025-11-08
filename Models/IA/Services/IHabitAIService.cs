using LifeHub.Models.IA.Results;

namespace LifeHub.Models.IA.Services
{
    public interface IHabitAIService
    {
        // Recomendaciones inteligentes
        Task<List<HabitRecommendation>> GeneratePersonalizedRecommendationsAsync(string userId);
        
        // Predicciones de éxito
        Task<HabitSuccessPrediction> PredictHabitSuccessAsync(string userId, string habitName, int frequency);
        
        // Detección de patrones
        Task<List<HabitPattern>> DetectHabitPatternsAsync(string userId);
        
        // Optimización de horarios
        Task<TimeOptimization> GetOptimalTimeForHabitAsync(string userId, string habitCategory);
        
        // Alertas de consistencia
        Task<List<ConsistencyAlert>> GetConsistencyAlertsAsync(string userId);
    }
}