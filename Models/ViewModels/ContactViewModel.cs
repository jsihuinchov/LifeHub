using System.ComponentModel.DataAnnotations;

namespace LifeHub.Models.ViewModels
{
    public class ContactViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de consulta es obligatorio")]
        [Display(Name = "Tipo de consulta")]
        public TipoConsulta TipoConsulta { get; set; }

        [Required(ErrorMessage = "El asunto es obligatorio")]
        [Display(Name = "Asunto")]
        public string Asunto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El mensaje es obligatorio")]
        [Display(Name = "Mensaje")]
        [StringLength(1000, ErrorMessage = "El mensaje no puede exceder los 1000 caracteres")]
        public string Mensaje { get; set; } = string.Empty;

        // Para consultas técnicas
        [Display(Name = "URL de la página con problemas (opcional)")]
        public string? UrlProblema { get; set; }

        // Para consultas de planes
        [Display(Name = "Plan de interés")]
        public string? PlanInteres { get; set; }

        // Propiedades para el sistema
        public DateTime FechaEnvio { get; set; } = DateTime.Now;
        public string? IPAddress { get; set; }
    }

    public enum TipoConsulta
    {
        [Display(Name = "Soporte Técnico")]
        SoporteTecnico,
        
        [Display(Name = "Información de Planes")]
        InformacionPlanes,
        
        [Display(Name = "Facturación y Pagos")]
        FacturacionPagos,
        
        [Display(Name = "Colaboraciones")]
        Colaboraciones,
        
        [Display(Name = "Prensa y Medios")]
        PrensaMedios,
        
        [Display(Name = "Quejas y Sugerencias")]
        QuejasSugerencias,
        
        [Display(Name = "Reportar Bug/Error")]
        ReportarBug,
        
        [Display(Name = "Solicitar Demostración")]
        Demostracion,
        
        [Display(Name = "Otro")]
        Otro
    }
}