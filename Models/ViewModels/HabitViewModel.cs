using System.ComponentModel.DataAnnotations;

namespace LifeHub.Models.ViewModels
{
    public class HabitViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "El nombre del hábito es requerido")]
        [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public string? Category { get; set; }
        
        [Required(ErrorMessage = "La frecuencia es requerida")]
        [Range(1, 7, ErrorMessage = "La frecuencia debe ser entre 1 y 7 días por semana")]
        public int Frequency { get; set; } = 7;
        
        [Range(1, 100, ErrorMessage = "El objetivo debe ser entre 1 y 100")]
        public int? TargetCount { get; set; }
        
        public string ColorCode { get; set; } = "#3B82F6";
        public string Icon { get; set; } = "📝";
        
        // ✅ NUEVO: Propiedad para favorito
        public bool IsFavorite { get; set; } = false;
        
        // Para el formulario
        public List<string> AvailableCategories { get; set; } = new List<string> 
        { 
            "Salud", "Ejercicio", "Productividad", "Estudio", "Meditación", 
            "Lectura", "Alimentación", "Finanzas", "Social", "Otros" 
        };
        
        public List<string> AvailableIcons { get; set; } = new List<string>
        {
            "📝", "🏃", "📚", "💪", "🧘", "🍎", "💰", "👥", "🎯", "⭐", "🔥", "💡"
        };
    }
}