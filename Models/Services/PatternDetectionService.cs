using Microsoft.EntityFrameworkCore;
using LifeHub.Data;
using LifeHub.Models.Entities;

namespace LifeHub.Models.Services
{
    public class PatternDetectionService : IPatternDetectionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PatternDetectionService> _logger;

        public PatternDetectionService(ApplicationDbContext context, ILogger<PatternDetectionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<HealthPattern>> AnalyzePatternsAsync(string userId)
        {
            var patterns = new List<HealthPattern>();
            var wellnessData = await _context.WellnessChecks
                .Where(w => w.UserId == userId && w.CheckDate >= DateTime.UtcNow.AddDays(-60))
                .ToListAsync();

            if (!wellnessData.Any())
                return patterns;

            // Detectar clusters de síntomas
            var symptomClusters = await DetectSymptomClustersAsync(wellnessData);
            foreach (var cluster in symptomClusters.Symptoms)
            {
                patterns.Add(new HealthPattern
                {
                    PatternType = "SymptomCluster",
                    Description = $"Síntomas que suelen aparecer juntos: {cluster}",
                    Confidence = 0.75,
                    RelatedFactors = new List<string> { "Síntomas concurrentes", "Patrón de malestar" },
                    DetectedAt = DateTime.UtcNow
                });
            }

            // Detectar patrones temporales
            var temporalPatterns = await FindTemporalPatternsAsync(wellnessData);
            foreach (var pattern in temporalPatterns)
            {
                patterns.Add(new HealthPattern
                {
                    PatternType = "Temporal",
                    Description = pattern.Description,
                    Confidence = 0.70,
                    RelatedFactors = new List<string> { pattern.TimeFrame, "Rutina diaria" },
                    DetectedAt = DateTime.UtcNow
                });
            }

            // Detectar correlaciones
            var correlations = await FindCorrelationsAsync(wellnessData);
            foreach (var correlation in correlations.Take(3))
            {
                patterns.Add(new HealthPattern
                {
                    PatternType = "Correlation",
                    Description = $"{correlation.Factor1} está relacionado con {correlation.Factor2}",
                    Confidence = correlation.Strength,
                    RelatedFactors = new List<string> { correlation.Factor1, correlation.Factor2 },
                    DetectedAt = DateTime.UtcNow
                });
            }

            return patterns;
        }

        public async Task<List<Correlation>> FindCorrelationsAsync(List<WellnessCheck> data)
        {
            var correlations = new List<Correlation>();

            if (data.Count < 5) return correlations;

            // Correlación sueño ↔ energía
            var sleepEnergyCorrelation = CalculateCorrelation(
                data.Select(d => (double)d.SleepQuality).ToArray(),
                data.Select(d => (double)d.EnergyLevel).ToArray()
            );

            if (Math.Abs(sleepEnergyCorrelation) > 0.5)
            {
                correlations.Add(new Correlation
                {
                    Factor1 = "Calidad de Sueño",
                    Factor2 = "Nivel de Energía",
                    Strength = Math.Abs(sleepEnergyCorrelation),
                    Direction = sleepEnergyCorrelation > 0 ? "positive" : "negative"
                });
            }

            // Correlación síntomas ↔ energía
            var highSymptomDays = data.Where(d => d.GetSymptomsList().Count > 2).ToList();
            if (highSymptomDays.Any())
            {
                var avgEnergyWithSymptoms = highSymptomDays.Average(d => d.EnergyLevel);
                var avgEnergyWithoutSymptoms = data.Where(d => d.GetSymptomsList().Count <= 2)
                                                 .Average(d => d.EnergyLevel);

                var symptomEnergyImpact = 1.0 - (avgEnergyWithSymptoms / avgEnergyWithoutSymptoms);
                
                if (symptomEnergyImpact > 0.2)
                {
                    correlations.Add(new Correlation
                    {
                        Factor1 = "Múltiples Síntomas",
                        Factor2 = "Energía Reducida",
                        Strength = symptomEnergyImpact,
                        Direction = "negative"
                    });
                }
            }

            return correlations;
        }

        public async Task<List<string>> GeneratePredictiveInsightsAsync(string userId)
        {
            var insights = new List<string>();
            var wellnessData = await _context.WellnessChecks
                .Where(w => w.UserId == userId && w.CheckDate >= DateTime.UtcNow.AddDays(-30))
                .ToListAsync();

            if (!wellnessData.Any())
            {
                insights.Add("Continúa registrando tu bienestar para obtener insights predictivos.");
                return insights;
            }

            // Análisis de tendencias de energía
            var recentEnergy = wellnessData.Where(w => w.CheckDate >= DateTime.UtcNow.AddDays(-7))
                                         .Average(w => w.EnergyLevel);
            var previousEnergy = wellnessData.Where(w => w.CheckDate < DateTime.UtcNow.AddDays(-7) && 
                                                       w.CheckDate >= DateTime.UtcNow.AddDays(-14))
                                           .Average(w => w.EnergyLevel);

            if (recentEnergy < previousEnergy * 0.8)
                insights.Add("📉 Tu energía ha disminuido recientemente. Considera revisar tus hábitos de descanso.");

            // Detección de patrones de síntomas recurrentes
            var frequentSymptoms = wellnessData
                .SelectMany(w => w.GetSymptomsList())
                .GroupBy(s => s)
                .Where(g => g.Count() >= 3)
                .Select(g => g.Key)
                .ToList();

            if (frequentSymptoms.Any())
            {
                var symptomNames = string.Join(", ", frequentSymptoms.Select(s => 
                    new WellnessCheck().GetSymptomDisplayName(s)));
                insights.Add($"🔍 Síntomas recurrentes detectados: {symptomNames}. Considera comentarlos con tu médico.");
            }

            // Análisis de consistencia en medicación
            var medicationConsistency = wellnessData.Count(w => w.TookMedications) / (double)wellnessData.Count;
            if (medicationConsistency < 0.7)
                insights.Add("💊 Tu consistencia con la medicación es baja. Establece recordatorios para mejorar.");

            return insights;
        }

        public async Task<SymptomCluster> DetectSymptomClustersAsync(List<WellnessCheck> data)
        {
            var cluster = new SymptomCluster { Name = "Cluster Principal" };

            var allSymptoms = data.SelectMany(w => w.GetSymptomsList()).ToList();
            var symptomGroups = allSymptoms.GroupBy(s => s)
                                         .Where(g => g.Count() >= 2)
                                         .OrderByDescending(g => g.Count())
                                         .Take(3)
                                         .ToList();

            cluster.Symptoms = symptomGroups.Select(g => g.Key.ToString()).ToList();
            cluster.Frequency = symptomGroups.Sum(g => g.Count());
            cluster.Severity = symptomGroups.Any() ? symptomGroups.Average(g => g.Count()) / (double)data.Count : 0;

            return cluster;
        }

        public async Task<List<TemporalPattern>> FindTemporalPatternsAsync(List<WellnessCheck> data)
        {
            var patterns = new List<TemporalPattern>();

            if (!data.Any()) return patterns;

            // Patrón de energía por día de la semana
            var weeklyEnergy = data.GroupBy(w => w.CheckDate.DayOfWeek)
                                  .Select(g => new { Day = g.Key, Energy = g.Average(w => w.EnergyLevel) })
                                  .OrderBy(x => x.Day)
                                  .ToList();

            var lowEnergyDays = weeklyEnergy.Where(x => x.Energy < 5).ToList();
            foreach (var day in lowEnergyDays)
            {
                patterns.Add(new TemporalPattern
                {
                    Pattern = "Energía Baja",
                    TimeFrame = day.Day.ToString(),
                    Description = $"Tu energía tiende a ser más baja los {GetDayName(day.Day)}"
                });
            }

            // Patrón de síntomas por momento del día (simulado)
            var morningSymptoms = data.Where(w => w.GetSymptomsList().Any()).Count();
            if (morningSymptoms > data.Count * 0.3)
            {
                patterns.Add(new TemporalPattern
                {
                    Pattern = "Síntomas Matutinos",
                    TimeFrame = "morning",
                    Description = "Los síntomas suelen aparecer con más frecuencia en las mañanas"
                });
            }

            return patterns;
        }

        private double CalculateCorrelation(double[] x, double[] y)
        {
            if (x.Length != y.Length || x.Length == 0)
                return 0;

            var avgX = x.Average();
            var avgY = y.Average();

            var numerator = x.Zip(y, (xi, yi) => (xi - avgX) * (yi - avgY)).Sum();
            var denominator = Math.Sqrt(x.Sum(xi => Math.Pow(xi - avgX, 2)) * y.Sum(yi => Math.Pow(yi - avgY, 2)));

            return denominator == 0 ? 0 : numerator / denominator;
        }

        private string GetDayName(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => "lunes",
                DayOfWeek.Tuesday => "martes",
                DayOfWeek.Wednesday => "miércoles",
                DayOfWeek.Thursday => "jueves",
                DayOfWeek.Friday => "viernes",
                DayOfWeek.Saturday => "sábados",
                DayOfWeek.Sunday => "domingos",
                _ => day.ToString()
            };
        }
    }
}