using LifeHub.Models;

namespace LifeHub.Models.ViewModels
{
    public class PaymentViewModel
    {
        public SubscriptionPlan Plan { get; set; } = null!;
        public string PaymentIntentClientSecret { get; set; } = string.Empty;
        public string StripePublishableKey { get; set; } = string.Empty;
    }
}