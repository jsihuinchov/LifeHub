// LifeHub/Models/Entities/HealthEnums.cs
namespace LifeHub.Models.Entities
{
    public enum WellnessLevel
    {
        Terrible = 1,
        Malo = 2, 
        Regular = 3,
        Bueno = 4,
        Excelente = 5
    }

    public enum HealthSymptom
    {
        Headache,           // Dolor de cabeza
        Fatigue,            // Fatiga
        Nausea,             // Náuseas
        Palpitations,       // Palpitaciones
        Fever,              // Fiebre
        LossOfAppetite,     // Falta de apetito
        Anxiety,            // Ansiedad
        Depression,         // Depresión
        SleepProblems,      // Problemas de sueño
        JointPain,          // Dolor articular
        Dizziness,          // Mareos
        Other               // Otro
    }
}