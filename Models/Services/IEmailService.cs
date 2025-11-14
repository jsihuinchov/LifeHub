using LifeHub.Models.ViewModels;

namespace LifeHub.Services
{
    public interface IEmailService
    {
        Task<bool> SendContactEmailAsync(ContactViewModel model);
        Task<bool> SendWelcomeEmailAsync(string toEmail, string userName);
        Task<bool> SendSupportResponseAsync(string toEmail, string userName, string subject, string message);
    
        Task<bool> SendSystemNotificationAsync(string toEmail, string subject, string htmlMessage);
    }
}