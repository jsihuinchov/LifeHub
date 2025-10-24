using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeHub.Models.Entities
{
    public class Budget
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int BudgetType { get; set; } // 0: Income, 1: Expense
        public decimal MonthlyAmount { get; set; }
        public decimal CurrentAmount { get; set; } = 0;
        public string MonthYear { get; set; } = string.Empty;
        public bool NotificationsEnabled { get; set; } = true;
        
        private DateTime _createdAt = DateTime.UtcNow;
        public DateTime CreatedAt 
        { 
            get => _createdAt;
            set => _createdAt = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        }
        
        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; }
    }
}