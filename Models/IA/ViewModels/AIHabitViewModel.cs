using LifeHub.Models.IA.Results;

namespace LifeHub.Models.IA.ViewModels
{
    public class AIHabitViewModel
    {
        public List<HabitRecommendation> Recommendations { get; set; } = new();
        public List<ConsistencyAlert> Alerts { get; set; } = new();
        public List<HabitPattern> Patterns { get; set; } = new();
        public bool HasAIFeatures { get; set; }
    }
}