using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeHub.Models.Entities
{
    [Table("users")] // 👈 asegura que la tabla se llame users en PostgreSQL
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("first_name")]
        public string? FirstName { get; set; }

        [Column("last_name")]
        public string? LastName { get; set; }

        [Column("date_of_birth")]
        public DateTime? DateOfBirth { get; set; }

        [Column("profile_picture_url")]
        public string? ProfilePictureUrl { get; set; }

        [Column("is_premium")]
        public bool IsPremium { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("last_login")]
        public DateTime? LastLogin { get; set; }

        [Column("stripe_customer_id")]
        public string? StripeCustomerId { get; set; }
    }
}
