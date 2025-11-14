using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeHub.Models.Entities
{
    public class WellnessCheck
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime CheckDate { get; set; } = DateTime.UtcNow.Date;
        
        // 🔄 MANTENER campo existente para compatibilidad
        public string Mood { get; set; } = "neutral"; // happy, neutral, sad
        
        // ✅ NUEVO campo para estado general (enum)
        public WellnessLevel GeneralWellness { get; set; } = WellnessLevel.Regular;
        
        // Campos existentes (MANTENER)
        public int EnergyLevel { get; set; } = 5; // 1-10
        public string? QuickNote { get; set; }
        public bool TookMedications { get; set; }
        public string? MedicationNotes { get; set; }
        
        // ✅ NUEVOS CAMPOS para Diario de Salud
        public string Symptoms { get; set; } = string.Empty; // "Headache,Fatigue,Anxiety"
        public string? CustomSymptom { get; set; }
        public int SleepQuality { get; set; } = 5; // 1-10
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; }

        // ✅ MÉTODOS HELPER para manejar síntomas
        public List<HealthSymptom> GetSymptomsList()
        {
            if (string.IsNullOrEmpty(Symptoms)) 
                return new List<HealthSymptom>();
            
            return Symptoms.Split(',')
                .Where(s => !string.IsNullOrEmpty(s) && Enum.TryParse<HealthSymptom>(s, out _))
                .Select(s => Enum.Parse<HealthSymptom>(s))
                .ToList();
        }

        public void SetSymptomsList(List<HealthSymptom> symptoms)
        {
            Symptoms = symptoms.Any() ? string.Join(",", symptoms.Select(s => s.ToString())) : "";
        }

        public void AddSymptom(HealthSymptom symptom)
        {
            var symptoms = GetSymptomsList();
            if (!symptoms.Contains(symptom))
            {
                symptoms.Add(symptom);
                SetSymptomsList(symptoms);
            }
        }

        public void RemoveSymptom(HealthSymptom symptom)
        {
            var symptoms = GetSymptomsList();
            symptoms.Remove(symptom);
            SetSymptomsList(symptoms);
        }

        // ✅ MÉTODO para obtener nombre display del síntoma
        public string GetSymptomDisplayName(HealthSymptom symptom)
        {
            return symptom switch
            {
                HealthSymptom.Headache => "🤕 Dolor de cabeza",
                HealthSymptom.Fatigue => "😴 Fatiga",
                HealthSymptom.Nausea => "🤢 Náuseas",
                HealthSymptom.Palpitations => "💓 Palpitaciones",
                HealthSymptom.Fever => "🥶 Fiebre",
                HealthSymptom.LossOfAppetite => "🍽️ Falta de apetito",
                HealthSymptom.Anxiety => "😰 Ansiedad",
                HealthSymptom.Depression => "😞 Depresión",
                HealthSymptom.SleepProblems => "💤 Problemas de sueño",
                HealthSymptom.JointPain => "🦵 Dolor articular",
                HealthSymptom.Dizziness => "🌀 Mareos",
                HealthSymptom.Other => "📝 Otro",
                _ => symptom.ToString()
            };
        }
    }
}