using System.ComponentModel.DataAnnotations;

namespace LifeHub.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // Para el plan seleccionado
        public int SelectedPlanId { get; set; } = 1;
        public string SelectedPlanName { get; set; } = "Plan Gratis";

        // ✅ NUEVO: Propiedad para términos y condiciones
        [Range(typeof(bool), "true", "true", ErrorMessage = "Debes aceptar los términos y condiciones")]
        public bool AcceptTerms { get; set; }
    }
}