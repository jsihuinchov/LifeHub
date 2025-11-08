using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;
using LifeHub.Models.Entities;
using LifeHub.Data;
using Microsoft.EntityFrameworkCore;
using LifeHub.Models.IA.DataModels;

namespace LifeHub.Models.IA.Services
{
    public interface IHabitMLService
    {
        float PredictHabitSuccess(string userId, string habitName, int frequency);
        void RetrainModelWithNewData(List<HabitCompletion> newCompletions);
    }

    public class HabitMLService : IHabitMLService
    {
        private readonly MLContext _mlContext;
        private readonly ApplicationDbContext _context;
        private ITransformer? _successPredictionModel;

        public HabitMLService(MLContext mlContext, ApplicationDbContext context)
        {
            _mlContext = mlContext;
            _context = context;
            LoadOrTrainModel();
        }

        public float PredictHabitSuccess(string userId, string habitName, int frequency)
        {
            try
            {
                if (_successPredictionModel == null)
                    return CalculateBasicSuccessRate(frequency);

                var userFeatures = ExtractUserFeatures(userId);
                var habitFeatures = ExtractHabitFeatures(habitName, frequency);
                
                var predictionEngine = _mlContext.Model.CreatePredictionEngine<HabitFeatures, SuccessPrediction>(_successPredictionModel);
                
                var features = new HabitFeatures
                {
                    Frequency = frequency,
                    UserConsistency = userFeatures.ConsistencyScore,
                    Category = habitFeatures.Category,
                    TimeOfDayPreference = userFeatures.BestTimeOfDay,
                    PreviousHabitsSuccessRate = userFeatures.SuccessRate,
                    HabitComplexity = habitFeatures.ComplexityScore,
                    Success = false // Esto no se usa en predicción
                };
                
                var prediction = predictionEngine.Predict(features);
                return prediction.Probability * 100;
            }
            catch (Exception ex)
            {
                // Fallback a cálculo básico si hay error
                return CalculateBasicSuccessRate(frequency);
            }
        }

        // ✅ IMPLEMENTACIONES COMPLETAS DE TODOS LOS MÉTODOS

        private UserFeatures ExtractUserFeatures(string userId)
        {
            var userHabits = GetUserHabits(userId);
            
            return new UserFeatures
            {
                ConsistencyScore = CalculateConsistency(userHabits),
                SuccessRate = CalculateSuccessRate(userHabits),
                BestTimeOfDay = DetectBestTimePattern(userHabits),
                TotalHabits = userHabits.Count
            };
        }

        private (float Category, float ComplexityScore) ExtractHabitFeatures(string habitName, int frequency)
        {
            // Categorías codificadas numéricamente
            var category = habitName.ToLower() switch
            {
                string s when s.Contains("ejercicio") || s.Contains("deporte") || s.Contains("gym") => 1.0f,
                string s when s.Contains("lectura") || s.Contains("leer") || s.Contains("estudio") => 2.0f,
                string s when s.Contains("meditación") || s.Contains("yoga") || s.Contains("mindfulness") => 3.0f,
                string s when s.Contains("agua") || s.Contains("hidratación") || s.Contains("beber") => 4.0f,
                string s when s.Contains("sueño") || s.Contains("dormir") || s.Contains("descanso") => 5.0f,
                _ => 0.0f
            };

            // Score de complejidad basado en frecuencia y categoría
            var complexityScore = (frequency / 7.0f) + (category * 0.1f);
            
            return (category, complexityScore);
        }

        private List<Habit> GetUserHabits(string userId)
        {
            return _context.Habits
                .Include(h => h.HabitCompletions)
                .Where(h => h.UserId == userId && h.IsActive)
                .ToList();
        }

        private float CalculateConsistency(List<Habit> habits)
        {
            if (!habits.Any()) return 0.5f; // Valor por defecto
            
            var totalCompletions = habits.Sum(h => h.HabitCompletions?.Count ?? 0);
            var totalExpected = habits.Sum(h => h.Frequency) * 4; // 4 semanas
            
            if (totalExpected == 0) return 0.5f;
            
            return Math.Min((float)totalCompletions / totalExpected, 1.0f);
        }

        private float CalculateSuccessRate(List<Habit> habits)
        {
            if (!habits.Any()) return 0.6f; // Valor por defecto
            
            var successfulHabits = habits.Count(h => 
                (h.HabitCompletions?.Count ?? 0) >= (h.Frequency * 0.7)); // 70% de éxito
            
            return (float)successfulHabits / habits.Count;
        }

        private float DetectBestTimePattern(List<Habit> habits)
        {
            // Por defecto, mañana (7 AM)
            if (!habits.Any()) return 7.0f;
            
            var allCompletions = habits
                .SelectMany(h => h.HabitCompletions ?? new List<HabitCompletion>())
                .Where(hc => hc.Completed)
                .ToList();

            if (!allCompletions.Any()) return 7.0f;
            
            var averageHour = allCompletions
                .Average(hc => hc.CompletionDate.Hour);
            
            return (float)averageHour;
        }

        private float CalculateBasicSuccessRate(int frequency)
        {
            return frequency switch
            {
                1 => 85f,
                2 => 78f,
                3 => 70f,
                4 => 65f,
                5 => 58f,
                6 => 52f,
                7 => 45f,
                _ => 60f
            };
        }

        private void LoadOrTrainModel()
        {
            var modelPath = GetModelPath();
            
            if (File.Exists(modelPath))
            {
                _successPredictionModel = _mlContext.Model.Load(modelPath, out _);
            }
            else
            {
                var trainingData = GenerateInitialTrainingData();
                TrainModel(trainingData);
            }
        }

        private string GetModelPath()
        {
            var modelsDir = Path.Combine(Directory.GetCurrentDirectory(), "MLModels");
            if (!Directory.Exists(modelsDir))
                Directory.CreateDirectory(modelsDir);
            
            return Path.Combine(modelsDir, "HabitSuccessModel.zip");
        }

        private List<HabitFeatures> GenerateInitialTrainingData()
        {
            // Datos de entrenamiento sintéticos basados en patrones conocidos
            var trainingData = new List<HabitFeatures>();
            var random = new Random();

            for (int i = 0; i < 1000; i++)
            {
                var frequency = random.Next(1, 8);
                var userConsistency = (float)random.NextDouble();
                var category = random.Next(0, 6);
                var timeOfDay = random.Next(0, 24);
                var previousSuccess = (float)random.NextDouble();
                var complexity = (frequency / 7.0f) + (category * 0.1f);

                // Simular éxito basado en características
                var successProbability = 0.3f + 
                    (frequency <= 3 ? 0.2f : 0f) +
                    (userConsistency * 0.3f) +
                    (previousSuccess * 0.2f);
                
                var success = random.NextDouble() < successProbability;

                trainingData.Add(new HabitFeatures
                {
                    Frequency = frequency,
                    UserConsistency = userConsistency,
                    Category = category,
                    TimeOfDayPreference = timeOfDay,
                    PreviousHabitsSuccessRate = previousSuccess,
                    HabitComplexity = complexity,
                    Success = success
                });
            }

            return trainingData;
        }

        private void TrainModel(List<HabitFeatures> trainingData)
        {
            var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);
            
            var pipeline = _mlContext.Transforms.Concatenate("Features",
                    nameof(HabitFeatures.Frequency),
                    nameof(HabitFeatures.UserConsistency),
                    nameof(HabitFeatures.Category),
                    nameof(HabitFeatures.TimeOfDayPreference),
                    nameof(HabitFeatures.PreviousHabitsSuccessRate),
                    nameof(HabitFeatures.HabitComplexity))
                .Append(_mlContext.BinaryClassification.Trainers.FastTree(
                    labelColumnName: nameof(HabitFeatures.Success)));
            
            _successPredictionModel = pipeline.Fit(dataView);
            
            // Guardar modelo entrenado
            _mlContext.Model.Save(_successPredictionModel, dataView.Schema, GetModelPath());
        }

        public void RetrainModelWithNewData(List<HabitCompletion> newCompletions)
        {
            var newTrainingData = ProcessNewDataForTraining(newCompletions);
            
            if (newTrainingData.Count > 50) // Reentrenar solo si hay suficientes datos nuevos
            {
                TrainModel(newTrainingData);
            }
        }

        private List<HabitFeatures> ProcessNewDataForTraining(List<HabitCompletion> newCompletions)
        {
            // Procesar datos reales para entrenamiento
            var trainingData = new List<HabitFeatures>();
            
            var groupedByUser = newCompletions
                .GroupBy(c => c.Habit?.UserId)
                .Where(g => g.Key != null);

            foreach (var userGroup in groupedByUser)
            {
                var userHabits = GetUserHabits(userGroup.Key!);
                var userFeatures = ExtractUserFeatures(userGroup.Key!);

                foreach (var completion in userGroup)
                {
                    if (completion.Habit != null)
                    {
                        var habitFeatures = ExtractHabitFeatures(completion.Habit.Name, completion.Habit.Frequency);
                        
                        trainingData.Add(new HabitFeatures
                        {
                            Frequency = completion.Habit.Frequency,
                            UserConsistency = userFeatures.ConsistencyScore,
                            Category = habitFeatures.Category,
                            TimeOfDayPreference = userFeatures.BestTimeOfDay,
                            PreviousHabitsSuccessRate = userFeatures.SuccessRate,
                            HabitComplexity = habitFeatures.ComplexityScore,
                            Success = completion.Completed
                        });
                    }
                }
            }

            return trainingData;
        }
    }
}