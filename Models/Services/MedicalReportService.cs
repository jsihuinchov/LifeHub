// LifeHub/Models/Services/MedicalReportService.cs
using Microsoft.EntityFrameworkCore;
using LifeHub.Data;
using LifeHub.Models.Entities;

namespace LifeHub.Models.Services
{
    public class MedicalReportService : IMedicalReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPatternDetectionService _patternService;
        private readonly ILogger<MedicalReportService> _logger;

        public MedicalReportService(ApplicationDbContext context, 
                                  IPatternDetectionService patternService,
                                  ILogger<MedicalReportService> logger)
        {
            _context = context;
            _patternService = patternService;
            _logger = logger;
        }

        public async Task<MedicalReport> GenerateReportAsync(string userId, DateRange range)
        {
            var report = new MedicalReport();
            
            var wellnessData = await _context.WellnessChecks
                .Where(w => w.UserId == userId && 
                           w.CheckDate >= range.StartDate && 
                           w.CheckDate <= range.EndDate)
                .OrderBy(w => w.CheckDate)
                .ToListAsync();

            if (!wellnessData.Any())
            {
                report.ExecutiveSummary = "No hay datos suficientes para el período seleccionado.";
                return report;
            }

            report.ExecutiveSummary = await GenerateExecutiveSummaryAsync(wellnessData);
            report.Trends = await IdentifyTrendsAsync(wellnessData);
            report.Patterns = await _patternService.AnalyzePatternsAsync(userId);
            report.Recommendations = await GenerateRecommendationsAsync(userId);
            report.Statistics = CalculateStatistics(wellnessData);

            return report;
        }

        public async Task<string> GenerateExecutiveSummaryAsync(List<WellnessCheck> data)
        {
            if (!data.Any()) return "Sin datos para análisis.";

            var avgEnergy = data.Average(d => d.EnergyLevel);
            var avgSleep = data.Average(d => d.SleepQuality);
            var symptomDays = data.Count(d => d.GetSymptomsList().Any());
            var medicationDays = data.Count(d => d.TookMedications);

            var summary = $@"
Resumen Ejecutivo de Salud - Período: {data.Min(d => d.CheckDate):dd/MM/yyyy} a {data.Max(d => d.CheckDate):dd/MM/yyyy}

Estado General:
- Energía promedio: {avgEnergy:F1}/10
- Calidad de sueño: {avgSleep:F1}/10
- Días con síntomas: {symptomDays} de {data.Count}
- Adherencia a medicación: {(medicationDays/(double)data.Count):P0}

";

            if (avgEnergy < 5) summary += "• Tu nivel de energía requiere atención. Considera mejorar hábitos de descanso.\n";
            if (avgSleep < 5) summary += "• La calidad de sueño puede optimizarse. Establece una rutina consistente.\n";
            if (symptomDays > data.Count * 0.3) summary += "• Los síntomas aparecen frecuentemente. Monitorea patrones específicos.\n";

            return summary;
        }

        public async Task<List<HealthTrend>> IdentifyTrendsAsync(List<WellnessCheck> data)
        {
            var trends = new List<HealthTrend>();

            if (data.Count < 7) return trends;

            // Dividir datos en períodos para comparación
            var halfPoint = data.Count / 2;
            var firstHalf = data.Take(halfPoint).ToList();
            var secondHalf = data.Skip(halfPoint).ToList();

            // Tendencia de energía
            var energyFirst = firstHalf.Average(d => d.EnergyLevel);
            var energySecond = secondHalf.Average(d => d.EnergyLevel);
            var energyChange = (energySecond - energyFirst) / energyFirst * 100;

            trends.Add(new HealthTrend
            {
                Metric = "Nivel de Energía",
                Direction = energyChange > 5 ? "improving" : energyChange < -5 ? "declining" : "stable",
                ChangePercentage = Math.Round(energyChange, 1),
                Description = energyChange > 5 ? "Tu energía muestra mejora" : 
                             energyChange < -5 ? "Tu energía ha disminuido" : "Tu energía se mantiene estable"
            });

            // Tendencia de sueño
            var sleepFirst = firstHalf.Average(d => d.SleepQuality);
            var sleepSecond = secondHalf.Average(d => d.SleepQuality);
            var sleepChange = (sleepSecond - sleepFirst) / sleepFirst * 100;

            trends.Add(new HealthTrend
            {
                Metric = "Calidad de Sueño",
                Direction = sleepChange > 5 ? "improving" : sleepChange < -5 ? "declining" : "stable",
                ChangePercentage = Math.Round(sleepChange, 1),
                Description = sleepChange > 5 ? "Tu sueño está mejorando" : 
                             sleepChange < -5 ? "Tu sueño necesita atención" : "Calidad de sueño estable"
            });

            // Tendencia de síntomas
            var symptomsFirst = firstHalf.Average(d => d.GetSymptomsList().Count);
            var symptomsSecond = secondHalf.Average(d => d.GetSymptomsList().Count);
            var symptomsChange = symptomsFirst > 0 ? (symptomsSecond - symptomsFirst) / symptomsFirst * 100 : 0;

            trends.Add(new HealthTrend
            {
                Metric = "Frecuencia de Síntomas",
                Direction = symptomsChange < -10 ? "improving" : symptomsChange > 10 ? "declining" : "stable",
                ChangePercentage = Math.Round(symptomsChange, 1),
                Description = symptomsChange < -10 ? "Menos síntomas reportados" : 
                             symptomsChange > 10 ? "Aumento en síntomas" : "Frecuencia de síntomas estable"
            });

            return trends;
        }

        public async Task<List<HealthRecommendation>> GenerateRecommendationsAsync(string userId)
        {
            var recommendations = new List<HealthRecommendation>();
            var wellnessData = await _context.WellnessChecks
                .Where(w => w.UserId == userId && w.CheckDate >= DateTime.UtcNow.AddDays(-30))
                .ToListAsync();

            if (!wellnessData.Any()) return recommendations;

            var avgEnergy = wellnessData.Average(w => w.EnergyLevel);
            var avgSleep = wellnessData.Average(w => w.SleepQuality);
            var medicationAdherence = wellnessData.Count(w => w.TookMedications) / (double)wellnessData.Count;

            // Recomendaciones basadas en energía
            if (avgEnergy < 5)
            {
                recommendations.Add(new HealthRecommendation
                {
                    Category = "Energía",
                    Description = "Nivel de energía consistentemente bajo",
                    Priority = "high",
                    ActionSteps = "Considera: 1) Revisar hábitos de sueño, 2) Aumentar actividad física moderada, 3) Consultar con médico si persiste"
                });
            }

            // Recomendaciones basadas en sueño
            if (avgSleep < 5)
            {
                recommendations.Add(new HealthRecommendation
                {
                    Category = "Sueño",
                    Description = "Calidad de sueño por debajo del óptimo",
                    Priority = "medium",
                    ActionSteps = "Sugerencias: 1) Establecer horario consistente, 2) Evitar pantallas antes de dormir, 3) Crear ambiente relajante"
                });
            }

            // Recomendaciones basadas en medicación
            if (medicationAdherence < 0.8)
            {
                recommendations.Add(new HealthRecommendation
                {
                    Category = "Medicación",
                    Description = "Baja adherencia a la medicación",
                    Priority = "high",
                    ActionSteps = "Acciones: 1) Configurar recordatorios, 2) Usir pastillero semanal, 3) Hablar con médico sobre dificultades"
                });
            }

            // Recomendación general de seguimiento
            recommendations.Add(new HealthRecommendation
            {
                Category = "Seguimiento",
                Description = "Monitoreo continuo recomendado",
                Priority = "low",
                ActionSteps = "Continúa registrando tu bienestar diario para detectar patrones y mejorar insights"
            });

            return recommendations;
        }

        private ReportStatistics CalculateStatistics(List<WellnessCheck> data)
        {
            return new ReportStatistics
            {
                AverageEnergy = Math.Round(data.Average(d => d.EnergyLevel), 1),
                AverageSleep = Math.Round(data.Average(d => d.SleepQuality), 1),
                TotalEntries = data.Count,
                SymptomDays = data.Count(d => d.GetSymptomsList().Any()),
                MedicationAdherence = data.Count > 0 ? data.Count(d => d.TookMedications) / (double)data.Count : 0
            };
        }
    }
}