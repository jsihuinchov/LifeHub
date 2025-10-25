using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeHub.Models.Entities
{
    public class Community
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public bool IsPublic { get; set; } = true;
        public bool IsPremiumOnly { get; set; }
        public string? OwnerId { get; set; } // Cambiado de int? a string?
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<CommunityMember>? Members { get; set; }
        
        [ForeignKey("OwnerId")]
        public virtual IdentityUser? Owner { get; set; } // Añadido
    }
}