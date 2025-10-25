using System.ComponentModel.DataAnnotations;

namespace LifeHub.Models.ViewModels.AccountViewModels
{
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
}