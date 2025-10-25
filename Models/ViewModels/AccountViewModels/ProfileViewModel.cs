using System.ComponentModel.DataAnnotations;

namespace LifeHub.Models.ViewModels.AccountViewModels
{
    public class ProfileViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public bool EmailConfirmed { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public string? WebsiteUrl { get; set; }
        public bool EmailNotifications { get; set; } = true;
        public bool PushNotifications { get; set; } = true;
        public string? CurrentPlan { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public bool IsAdmin { get; set; }
        public int HabitsCount { get; set; }
        public int TransactionsCount { get; set; }
        public DateTime MemberSince { get; set; }
    }
}