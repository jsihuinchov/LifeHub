using System.ComponentModel.DataAnnotations;

namespace LifeHub.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int BannedUsers { get; set; }
        public int TotalHabits { get; set; }
        public int TotalTransactions { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int NewUsersToday { get; set; }
        public int ActiveHabits { get; set; }
        public List<PlanDistributionItem> PlanDistribution { get; set; } = new();
        public List<GrowthStatsViewModel> GrowthStats { get; set; } = new();
    }

    public class PlanDistributionItem
    {
        public string PlanName { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public decimal Percentage { get; set; }
        public string Color { get; set; } = "#3B82F6";
    }

    public class AdminUserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
        public string SubscriptionPlan { get; set; } = "Sin plan";
        public DateTime? SubscriptionEndDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        
        // Propiedades calculadas
        public bool IsBanned => LockoutEnd.HasValue && LockoutEnd > DateTimeOffset.Now;
        public bool IsAdmin => Roles.Contains("Admin");
        public string Status => IsBanned ? "Baneado" : "Activo";
        public string StatusBadgeClass => IsBanned ? "badge bg-danger" : "badge bg-success";
        public int DaysUntilSubscriptionEnd => SubscriptionEndDate.HasValue ? 
            (SubscriptionEndDate.Value - DateTime.Now).Days : 0;
    }

    public class UserStatsViewModel
    {
        public string Period { get; set; } = string.Empty;
        public int NewUsers { get; set; }
        public int ActiveUsers { get; set; }
    }

    public class PlanDistributionViewModel
    {
        public List<PlanDistributionItem> Distribution { get; set; } = new();
        public int TotalUsers { get; set; }
    }

    public class UsageStatsViewModel
    {
        public int TotalHabitsCreated { get; set; }
        public int TotalTransactionsCreated { get; set; }
        public int TotalAppointmentsCreated { get; set; }
        public int TotalCommunitiesCreated { get; set; }
        public int ActiveHabitsToday { get; set; }
        public int TransactionsThisMonth { get; set; }
    }

    public class GrowthStatsViewModel
    {
        public string Date { get; set; } = string.Empty;
        public int NewUsers { get; set; }
        public int NewHabits { get; set; }
        public int NewTransactions { get; set; }
    }

    public class BanUserViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public bool Permanent { get; set; } = true;
        public int? Days { get; set; }
    }

    public class ChangePlanViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        [Required]
        public int PlanId { get; set; }
        public List<SubscriptionPlan> AvailablePlans { get; set; } = new();
    }
}   