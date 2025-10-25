using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeHub.Models.Entities
{
    [Table("user_profiles")]
    public class UserProfile
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        [Column("bio")]
        [MaxLength(500)]
        public string? Bio { get; set; }

        [Column("location")]
        [MaxLength(100)]
        public string? Location { get; set; }

        [Column("website_url")]
        [Url]
        [MaxLength(200)]
        public string? WebsiteUrl { get; set; }

        [Column("social_links")]
        public string? SocialLinks { get; set; }

        [Column("privacy_settings")]
        public string? PrivacySettings { get; set; }

        [Column("email_notifications")]
        public bool EmailNotifications { get; set; } = true;

        [Column("push_notifications")]
        public bool PushNotifications { get; set; } = true;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public Microsoft.AspNetCore.Identity.IdentityUser? User { get; set; }
    }
}