using Stripe;

namespace LifeHub.Services
{
    public interface IStripeService
    {
        Task<PaymentIntent> CreatePaymentIntentAsync(long amount, string currency, string customerEmail);
        Task<Customer> CreateCustomerAsync(string email, string name);
        Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId);
    }
}