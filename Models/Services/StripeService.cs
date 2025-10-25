using Stripe;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LifeHub.Services
{
    public class StripeService : IStripeService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripeService> _logger;

        public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<PaymentIntent> CreatePaymentIntentAsync(long amount, string currency, string customerEmail)
        {
            try
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = amount,
                    Currency = currency,
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "customer_email", customerEmail }
                    }
                };

                var service = new PaymentIntentService();
                return await service.CreateAsync(options);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error creating Stripe payment intent");
                throw;
            }
        }

        public async Task<Customer> CreateCustomerAsync(string email, string name)
        {
            try
            {
                var options = new CustomerCreateOptions
                {
                    Email = email,
                    Name = name
                };

                var service = new CustomerService();
                return await service.CreateAsync(options);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error creating Stripe customer");
                throw;
            }
        }

        public async Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId)
        {
            try
            {
                var service = new PaymentIntentService();
                return await service.GetAsync(paymentIntentId);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error retrieving Stripe payment intent");
                throw;
            }
        }
    }
}