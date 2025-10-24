using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifeHub.Models.Entities
{
    public class FinancialTransaction
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public int TransactionType { get; set; } // 0: Income, 1: Expense
        public string? Category { get; set; }
        public string? Subcategory { get; set; }
        
        private DateTime _transactionDate = DateTime.UtcNow;
        public DateTime TransactionDate 
        { 
            get => _transactionDate;
            set => _transactionDate = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        }
        
        public bool Recurring { get; set; }
        public string? RecurringFrequency { get; set; }
        public bool IsConfirmed { get; set; } = true;
        public string? MLCategory { get; set; }
        
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