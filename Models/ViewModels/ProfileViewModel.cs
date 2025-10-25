using System.ComponentModel.DataAnnotations;

namespace LifeHub.Models.ViewModels
{
    public class ProfileViewModel
    {
        // Información básica del usuario
        public string Email { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public bool EmailConfirmed { get; set; }
        
        // Información del perfil
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? WebsiteUrl { get; set; }
        
        // Configuraciones
        public bool EmailNotifications { get; set; } = true;
        public bool PushNotifications { get; set; } = true;
        
        // Información de suscripción
        public string? CurrentPlan { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public bool IsAdmin { get; set; }
        
        // Estadísticas (opcional)
        public int HabitsCount { get; set; }
        public int TransactionsCount { get; set; }
        public DateTime MemberSince { get; set; }
    }

    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Nombre de usuario")]
        [MaxLength(50, ErrorMessage = "El nombre de usuario no puede exceder 50 caracteres")]
        public string? UserName { get; set; }

        [Display(Name = "Biografía")]
        [MaxLength(500, ErrorMessage = "La biografía no puede exceder 500 caracteres")]
        public string? Bio { get; set; }

        [Display(Name = "Ubicación")]
        [MaxLength(100, ErrorMessage = "La ubicación no puede exceder 100 caracteres")]
        public string? Location { get; set; }

        [Display(Name = "Sitio web")]
        [Url(ErrorMessage = "La URL del sitio web no es válida")]
        [MaxLength(200, ErrorMessage = "La URL no puede exceder 200 caracteres")]
        public string? WebsiteUrl { get; set; }

        [Display(Name = "Notificaciones por email")]
        public bool EmailNotifications { get; set; } = true;

        [Display(Name = "Notificaciones push")]
        public bool PushNotifications { get; set; } = true;
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "La contraseña actual es requerida")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es requerida")]
        [StringLength(100, ErrorMessage = "La contraseña debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nueva contraseña")]
        [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}