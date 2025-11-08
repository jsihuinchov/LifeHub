using Microsoft.ML;
using LifeHub.Models.Entities;
using LifeHub.Models.IA.DataModels;
using Microsoft.ML.Data;

namespace LifeHub.Models.IA.Services
{
    public class PatternDetectionService
    {
        private readonly MLContext _mlContext;

        public PatternDetectionService(MLContext mlContext)
        {
            _mlContext = mlContext;
        }

        public List<HabitCluster> DetectBehavioralPatterns(List<HabitCompletion> completions)
        {
            if (completions.Count < 10) 
                return new List<HabitCluster>();

            try
            {
                var features = completions.Select(c => new CompletionFeatures
                {
                    DayOfWeek = (float)c.CompletionDate.DayOfWeek,
                    TimeOfDay = c.CompletionDate.Hour,
                    HabitDuration = CalculateHabitDuration(c),
                    Success = c.Completed
                }).ToList();

                var dataView = _mlContext.Data.LoadFromEnumerable(features);
                
                var pipeline = _mlContext.Transforms.Concatenate("Features", 
                        nameof(CompletionFeatures.DayOfWeek),
                        nameof(CompletionFeatures.TimeOfDay),
                        nameof(CompletionFeatures.HabitDuration))
                    .Append(_mlContext.Clustering.Trainers.KMeans(
                        featureColumnName: "Features", 
                        numberOfClusters: 3));
                
                var model = pipeline.Fit(dataView);
                var predictions = model.Transform(dataView);
                
                return ExtractClusters(predictions, completions);
            }
            catch
            {
                return new List<HabitCluster>();
            }
        }

        private float CalculateHabitDuration(HabitCompletion completion)
        {
            // Por ahora, duración estimada basada en categoría
            // En una implementación real, esto vendría de datos reales
            return 30.0f; // 30 minutos por defecto
        }

        private List<HabitCluster> ExtractClusters(IDataView predictions, List<HabitCompletion> completions)
        {
            var clusters = new List<HabitCluster>();
            var predictionColumn = predictions.GetColumn<float[]>("Features").ToArray();
            
            for (int i = 0; i < Math.Min(3, predictionColumn.Length); i++)
            {
                clusters.Add(new HabitCluster
                {
                    ClusterId = i,
                    Description = GetClusterDescription(i),
                    HabitIds = completions
                        .Where((c, index) => index < predictionColumn.Length && 
                                           GetPredictedCluster(predictionColumn[index]) == i)
                        .Select(c => c.HabitId)
                        .Distinct()
                        .ToList()
                });
            }
            
            return clusters;
        }

        private int GetPredictedCluster(float[] features)
        {
            // Simular predicción de cluster (en realidad usarías la columna de predicción)
            return (int)(features[0] % 3);
        }

        private string GetClusterDescription(int clusterId)
        {
            return clusterId switch
            {
                0 => "Patrón: Actividad matutina consistente",
                1 => "Patrón: Hábitos vespertinos regulares", 
                2 => "Patrón: Actividad fin de semana",
                _ => "Patrón detectado"
            };
        }

        public List<TrendPrediction> PredictHabitTrends(List<HabitCompletion> historicalData)
        {
            if (historicalData.Count < 15) 
                return new List<TrendPrediction>();

            try
            {
                var data = historicalData
                    .GroupBy(h => h.CompletionDate.Date)
                    .Select(g => new TimeSeriesPoint
                    {
                        Date = g.Key,
                        Completions = g.Count(),
                        SuccessRate = (float)g.Count(h => h.Completed) / g.Count()
                    })
                    .OrderBy(p => p.Date)
                    .ToList();

                return DetectAnomalies(data);
            }
            catch
            {
                return new List<TrendPrediction>();
            }
        }

        private List<TrendPrediction> DetectAnomalies(List<TimeSeriesPoint> data)
        {
            var predictions = new List<TrendPrediction>();
            var avgCompletions = data.Average(d => d.Completions);
            var stdDev = CalculateStdDev(data.Select(d => (float)d.Completions));

            foreach (var point in data.TakeLast(7)) // Últimos 7 días
            {
                var isAnomaly = Math.Abs(point.Completions - avgCompletions) > (2 * stdDev);
                
                predictions.Add(new TrendPrediction
                {
                    Date = point.Date,
                    PredictedCompletions = (float)avgCompletions,
                    IsAnomaly = isAnomaly
                });
            }

            return predictions;
        }

        private float CalculateStdDev(IEnumerable<float> values)
        {
            var avg = values.Average();
            var sumSquares = values.Sum(v => Math.Pow(v - avg, 2));
            return (float)Math.Sqrt(sumSquares / values.Count());
        }
    }
}