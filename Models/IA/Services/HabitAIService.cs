using Microsoft.ML;
using LifeHub.Models.Entities;
using LifeHub.Models.Services;
using LifeHub.Data;
using Microsoft.EntityFrameworkCore;
using LifeHub.Services;
using LifeHub.Models.IA.Results;
using LifeHub.Models.ViewModels;

namespace LifeHub.Models.IA.Services
{
    public class HabitAIService : IHabitAIService
    {
        private readonly MLContext _mlContext;
        private readonly ApplicationDbContext _context;
        private readonly IHabitService _habitService;

        public HabitAIService(MLContext mlContext, ApplicationDbContext context, IHabitService habitService)
        {
            _mlContext = mlContext;
            _context = context;
            _habitService = habitService;
        }

        public async Task<List<HabitRecommendation>> GeneratePersonalizedRecommendationsAsync(string userId)
        {
            var recommendations = new List<HabitRecommendation>();
            var habits = await _habitService.GetUserHabitsAsync(userId);
            var stats = await _habitService.GetHabitStatsAsync(userId);

            // 1. Análisis de hábitos existentes
            foreach (var habit in habits)
            {
                var habitStats = await _habitService.GetHabitDetailStatsAsync(habit.Id, userId);
                if (habitStats == null) continue;

                // Recomendación basada en tasa de éxito
                if (habitStats.SuccessRate < 50)
                {
                    recommendations.Add(new HabitRecommendation
                    {
                        HabitName = habit.Name,
                        Recommendation = $"Considera reducir la frecuencia de {habit.Frequency} a {Math.Max(1, habit.Frequency - 2)} días por semana para mejorar la consistencia",
                        Type = "Consistency",
                        Confidence = 0.85,
                        Reason = $"Tasa de éxito actual: {habitStats.SuccessRate:F1}%"
                    });
                }

                // Recomendación basada en racha actual
                if (habitStats.CurrentStreak >= 7 && habitStats.SuccessRate > 80)
                {
                    recommendations.Add(new HabitRecommendation
                    {
                        HabitName = habit.Name,
                        Recommendation = "¡Excelente racha! Considera aumentar la dificultad o agregar una variante",
                        Type = "Progression",
                        Confidence = 0.90,
                        Reason = $"Racha actual: {habitStats.CurrentStreak} días, Éxito: {habitStats.SuccessRate:F1}%"
                    });
                }
            }

            // 2. Recomendaciones de nuevos hábitos basadas en categorías existentes
            var userCategories = habits.Where(h => !string.IsNullOrEmpty(h.Category))
                                     .GroupBy(h => h.Category!)
                                     .OrderByDescending(g => g.Count())
                                     .Take(3);

            foreach (var category in userCategories)
            {
                var suggestedHabits = GetSuggestedHabitsForCategory(category.Key);
                recommendations.AddRange(suggestedHabits);
            }

            // 3. Recomendación basada en días de la semana más exitosos
            var bestDay = await GetBestPerformanceDayAsync(userId);
            if (!string.IsNullOrEmpty(bestDay))
            {
                recommendations.Add(new HabitRecommendation
                {
                    HabitName = "Todos los hábitos",
                    Recommendation = $"Programa hábitos importantes los {bestDay} para maximizar tu éxito",
                    Type = "TimeOptimization",
                    Confidence = 0.75,
                    Reason = $"Día de mayor productividad detectado: {bestDay}"
                });
            }

            return recommendations.OrderByDescending(r => r.Confidence).Take(5).ToList();
        }

        public async Task<HabitSuccessPrediction> PredictHabitSuccessAsync(string userId, string habitName, int frequency)
        {
            var userHabits = await _habitService.GetUserHabitsAsync(userId);
            var userStats = await _habitService.GetHabitStatsAsync(userId);

            // Factores para la predicción
            var overallSuccessRate = userStats.CompletionRate;
            var similarHabitsSuccess = userHabits
                .Where(h => h.Category == "Similar") // Aquí podrías implementar categorización más inteligente
                .Select(async h => await _habitService.GetHabitDetailStatsAsync(h.Id, userId))
                .Where(h => h != null && h.Result?.SuccessRate > 0)
                .Average(h => h.Result?.SuccessRate) ?? overallSuccessRate;

            var currentStreak = userStats.CurrentStreak;
            var consistencyScore = userStats.WeeklyConsistency;

            // Lógica de predicción simplificada (luego implementaremos ML)
            var baseProbability = (overallSuccessRate / 100.0) * 0.6 + 
                                (similarHabitsSuccess / 100.0) * 0.3 + 
                                (Math.Min(consistencyScore / 100.0, 1.0)) * 0.1;

            // Ajustar por frecuencia
            var frequencyFactor = frequency switch
            {
                1 => 0.9,
                2 => 0.85,
                3 => 0.8,
                4 => 0.75,
                5 => 0.7,
                6 => 0.65,
                7 => 0.6,
                _ => 0.7
            };

            var successProbability = baseProbability * frequencyFactor;

            return new HabitSuccessPrediction
            {
                SuccessProbability = successProbability * 100,
                ConfidenceLevel = successProbability > 0.7 ? "High" : successProbability > 0.5 ? "Medium" : "Low",
                KeyFactors = GetKeySuccessFactors(successProbability, userStats),
                Recommendation = GenerateSuccessRecommendation(successProbability, frequency)
            };
        }

        public async Task<List<HabitPattern>> DetectHabitPatternsAsync(string userId)
        {
            var patterns = new List<HabitPattern>();
            var habits = await _habitService.GetUserHabitsAsync(userId);

            // Patrón 1: Análisis de consistencia semanal
            var weeklyPattern = await DetectWeeklyPatternAsync(userId);
            if (weeklyPattern != null)
            {
                patterns.Add(weeklyPattern);
            }

            // Patrón 2: Análisis por categoría
            var categoryPattern = await DetectCategoryPatternAsync(userId);
            if (categoryPattern != null)
            {
                patterns.Add(categoryPattern);
            }

            // Patrón 3: Análisis de progresión temporal
            var progressionPattern = await DetectProgressionPatternAsync(userId);
            if (progressionPattern != null)
            {
                patterns.Add(progressionPattern);
            }

            return patterns;
        }

        public async Task<TimeOptimization> GetOptimalTimeForHabitAsync(string userId, string habitCategory)
        {
            var completions = await _context.HabitCompletions
                .Include(hc => hc.Habit)
                .Where(hc => hc.Habit.UserId == userId && 
                           hc.Habit.Category == habitCategory && 
                           hc.Completed)
                .ToListAsync();

            if (!completions.Any())
            {
                return new TimeOptimization
                {
                    BestTimeOfDay = "Morning",
                    BestDayOfWeek = "Monday",
                    ImprovementPotential = 0.5
                };
            }

            // Análisis de hora del día
            var timeGroups = completions.GroupBy(hc => hc.CompletionDate.Hour switch
            {
                >= 6 and < 12 => "Morning",
                >= 12 and < 18 => "Afternoon",
                >= 18 and < 24 => "Evening",
                _ => "Night"
            });

            var bestTime = timeGroups.OrderByDescending(g => g.Count()).First().Key;

            // Análisis de día de la semana
            var dayGroups = completions.GroupBy(hc => hc.CompletionDate.DayOfWeek.ToString());
            var bestDay = dayGroups.OrderByDescending(g => g.Count()).First().Key;

            return new TimeOptimization
            {
                BestTimeOfDay = bestTime,
                BestDayOfWeek = bestDay,
                ImprovementPotential = 0.7 // Podría calcularse basado en datos históricos
            };
        }

        public async Task<List<ConsistencyAlert>> GetConsistencyAlertsAsync(string userId)
        {
            var alerts = new List<ConsistencyAlert>();
            var habits = await _habitService.GetUserHabitsAsync(userId);

            foreach (var habit in habits)
            {
                var stats = await _habitService.GetHabitDetailStatsAsync(habit.Id, userId);
                if (stats == null) continue;

                if (stats.SuccessRate < 30)
                {
                    alerts.Add(new ConsistencyAlert
                    {
                        HabitName = habit.Name,
                        AlertType = "AtRisk",
                        Message = $"Este hábito está en riesgo de abandono. Tasa de éxito: {stats.SuccessRate:F1}%",
                        CurrentConsistency = stats.SuccessRate,
                        TargetConsistency = 70.0
                    });
                }
                else if (stats.SuccessRate < 60)
                {
                    alerts.Add(new ConsistencyAlert
                    {
                        HabitName = habit.Name,
                        AlertType = "Inconsistent",
                        Message = $"La consistencia puede mejorar. Considera ajustar la frecuencia u horario",
                        CurrentConsistency = stats.SuccessRate,
                        TargetConsistency = 70.0
                    });
                }

                // Alertas de racha en declive
                if (stats.CurrentStreak > 0 && stats.CurrentStreak < stats.LongestStreak / 2)
                {
                    alerts.Add(new ConsistencyAlert
                    {
                        HabitName = habit.Name,
                        AlertType = "Declining",
                        Message = $"La racha actual ({stats.CurrentStreak} días) es menor que tu mejor racha ({stats.LongestStreak} días)",
                        CurrentConsistency = stats.CurrentStreak,
                        TargetConsistency = stats.LongestStreak
                    });
                }
            }

            return alerts;
        }

        // Métodos auxiliares privados
        private List<HabitRecommendation> GetSuggestedHabitsForCategory(string category)
        {
            var suggestions = new Dictionary<string, List<string>>
            {
                ["Salud"] = new List<string> { "Meditación matutina", "Hidratación constante", "Estiramientos" },
                ["Ejercicio"] = new List<string> { "Cardio ligero", "Entrenamiento de fuerza", "Yoga" },
                ["Productividad"] = new List<string> { "Planificación diaria", "Revisión semanal", "Técnica Pomodoro" },
                ["Estudio"] = new List<string> { "Lectura técnica", "Práctica de habilidades", "Revisión espaciada" },
                ["Finanzas"] = new List<string> { "Registro de gastos", "Revisión de presupuesto", "Ahorro automático" }
            };

            if (suggestions.ContainsKey(category))
            {
                return suggestions[category].Select(habit => new HabitRecommendation
                {
                    HabitName = habit,
                    Recommendation = $"Basado en tu interés en {category}, este hábito podría complementar tu rutina",
                    Type = "NewHabit",
                    Confidence = 0.7,
                    Reason = $"Categoría relacionada: {category}"
                }).ToList();
            }

            return new List<HabitRecommendation>();
        }

        private async Task<string?> GetBestPerformanceDayAsync(string userId)
        {
            var completions = await _context.HabitCompletions
                .Include(hc => hc.Habit)
                .Where(hc => hc.Habit.UserId == userId && hc.Completed)
                .ToListAsync();

            if (!completions.Any()) return null;

            var dayPerformance = completions
                .GroupBy(hc => hc.CompletionDate.DayOfWeek)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .First();

            return dayPerformance.Day.ToString();
        }

        private List<string> GetKeySuccessFactors(double probability, HabitStatsViewModel stats)
        {
            var factors = new List<string>();

            if (stats.WeeklyConsistency > 70)
                factors.Add("Alta consistencia semanal");
            else
                factors.Add("Consistencia semanal a mejorar");

            if (stats.CurrentStreak > 3)
                factors.Add("Racha actual positiva");
            else
                factors.Add("Racha actual en construcción");

            if (stats.CompletionRate > 60)
                factors.Add("Buena tasa de completitud general");
            else
                factors.Add("Tasa de completitud a mejorar");

            return factors;
        }

        private string GenerateSuccessRecommendation(double probability, int frequency)
        {
            if (probability > 0.7)
                return $"Con {frequency} días/semana tienes alta probabilidad de éxito. ¡Mantén la consistencia!";
            else if (probability > 0.5)
                return $"Considera empezar con {Math.Max(1, frequency - 1)} días/semana y aumentar gradualmente";
            else
                return $"Recomendamos empezar con 2-3 días/semana para construir el hábito sólidamente";
        }

        private async Task<HabitPattern?> DetectWeeklyPatternAsync(string userId)
        {
            var completions = await _context.HabitCompletions
                .Include(hc => hc.Habit)
                .Where(hc => hc.Habit.UserId == userId && hc.Completed)
                .ToListAsync();

            if (!completions.Any()) return null;

            var weeklyDistribution = completions
                .GroupBy(hc => hc.CompletionDate.DayOfWeek)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            var mostProductiveDay = weeklyDistribution.OrderByDescending(kv => kv.Value).First();
            var leastProductiveDay = weeklyDistribution.OrderBy(kv => kv.Value).First();

            return new HabitPattern
            {
                PatternType = "Weekly",
                Description = $"Mayor productividad los {mostProductiveDay.Key}, menor los {leastProductiveDay.Key}",
                Strength = (double)mostProductiveDay.Value / (leastProductiveDay.Value == 0 ? 1 : leastProductiveDay.Value),
                SuggestedAction = $"Enfoca hábitos desafiantes los {mostProductiveDay.Key}"
            };
        }

        private async Task<HabitPattern?> DetectCategoryPatternAsync(string userId)
        {
            var habits = await _habitService.GetUserHabitsAsync(userId);
            var categorySuccess = new Dictionary<string, double>();

            foreach (var habit in habits.Where(h => !string.IsNullOrEmpty(h.Category)))
            {
                var stats = await _habitService.GetHabitDetailStatsAsync(habit.Id, userId);
                if (stats != null)
                {
                    if (!categorySuccess.ContainsKey(habit.Category!))
                        categorySuccess[habit.Category!] = 0;
                    
                    categorySuccess[habit.Category!] += stats.SuccessRate;
                }
            }

            if (!categorySuccess.Any()) return null;

            var bestCategory = categorySuccess.OrderByDescending(kv => kv.Value).First();
            var worstCategory = categorySuccess.OrderBy(kv => kv.Value).First();

            return new HabitPattern
            {
                PatternType = "CategoryBased",
                Description = $"Mejor desempeño en {bestCategory.Key}, más desafío en {worstCategory.Key}",
                Strength = bestCategory.Value / (worstCategory.Value == 0 ? 1 : worstCategory.Value),
                SuggestedAction = $"Refuerza tus habilidades en {worstCategory.Key} con hábitos más simples"
            };
        }

        private async Task<HabitPattern?> DetectProgressionPatternAsync(string userId)
        {
            var habits = await _habitService.GetUserHabitsAsync(userId);
            var improvementRates = new List<double>();

            foreach (var habit in habits)
            {
                var stats = await _habitService.GetHabitDetailStatsAsync(habit.Id, userId);
                if (stats != null && stats.MonthCompletions > 0)
                {
                    // Calcular mejora mensual (simplificado)
                    var improvement = (double)stats.MonthCompletions / Math.Max(1, stats.TotalCompletions - stats.MonthCompletions);
                    improvementRates.Add(improvement);
                }
            }

            if (!improvementRates.Any()) return null;

            var avgImprovement = improvementRates.Average();
            var trend = avgImprovement > 1.1 ? "positiva" : avgImprovement < 0.9 ? "negativa" : "estable";

            return new HabitPattern
            {
                PatternType = "Progression",
                Description = $"Tendencia {trend} en tu progreso de hábitos",
                Strength = avgImprovement,
                SuggestedAction = trend == "positiva" 
                    ? "¡Excelente! Considera objetivos más ambiciosos" 
                    : "Enfócate en consolidar los hábitos actuales"
            };
        }
    }
}