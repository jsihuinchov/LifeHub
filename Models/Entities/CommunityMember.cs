using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeHub.Models.Entities
{
    public class CommunityMember
    {
        public int Id { get; set; }
        public int CommunityId { get; set; }
        public string UserId { get; set; } = string.Empty; // Cambiado de int a string
        public int Role { get; set; } // 0: Member, 1: Admin, 2: Owner
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        
        public Community? Community { get; set; }
        
        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; } // Añadido
    }
}