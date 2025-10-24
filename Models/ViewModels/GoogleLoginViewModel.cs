using System.ComponentModel.DataAnnotations;

namespace LifeHub.Models.ViewModels
{
    public class GoogleLoginViewModel
    {
        [Required]
        public string IdToken { get; set; } = string.Empty;
        
        public string? ReturnUrl { get; set; }
    }

    public class GoogleUserInfo
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Picture { get; set; } = string.Empty;
        public string GivenName { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public string Sub { get; set; } = string.Empty;
    }
}