using System;
using LifeHub.Models; // Añadir este using

namespace LifeHub.Models
{
    public class UserSubscription
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int PlanId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Propiedades de navegación
        public SubscriptionPlan? Plan { get; set; }
        public Microsoft.AspNetCore.Identity.IdentityUser? User { get; set; }
        
        // Propiedades calculadas
        public bool IsExpired => EndDate < DateTime.UtcNow;
        public int DaysRemaining => (EndDate - DateTime.UtcNow).Days;
    }
}