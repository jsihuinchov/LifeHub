using LifeHub.Models.Entities;

namespace LifeHub.Models.Services
{
    public interface IChatbotService
    {
        Task<ChatResponse> ProcessMessageAsync(string userId, string userMessage);
        Task<ChatResponse> StartAppointmentPreparationAsync(int appointmentId);
        Task<List<ChatMessage>> GetConversationHistoryAsync(string userId);
        Task ClearConversationHistoryAsync(string userId);
    }

    public class ChatResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<QuickAction> QuickActions { get; set; } = new();
        public bool IsComplete { get; set; }
        public string NextStep { get; set; } = string.Empty;
        public AppointmentPreparation? Preparation { get; set; }
    }

    public class ChatMessage
    {
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsUser { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? QuickActions { get; set; }
    }

    public class QuickAction
    {
        public string Text { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "button", "suggestion"
    }
}