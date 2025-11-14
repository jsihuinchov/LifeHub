using LifeHub.Models.Entities;

namespace LifeHub.Models.Services
{
    public interface IAppointmentChatbotService
    {
        Task<AppointmentPreparation> PrepareForAppointmentAsync(int appointmentId);
        Task<List<string>> GenerateQuestionsForDoctorAsync(string specialty, List<string> currentSymptoms);
        Task<Checklist> GenerateChecklistAsync(string appointmentType, List<string> healthHistory);
        Task<string> GenerateConversationStarterAsync(string doctorName, string specialty);
    }

    public class AppointmentPreparation
    {
        public List<string> QuestionsForDoctor { get; set; } = new();
        public List<string> DocumentsToBring { get; set; } = new();
        public List<string> PreparationSteps { get; set; } = new();
        public string SpecialInstructions { get; set; } = string.Empty;
        public Checklist Checklist { get; set; } = new();
        public string ConversationStarter { get; set; } = string.Empty;
    }

    public class Checklist
    {
        public List<ChecklistItem> Items { get; set; } = new();
        public string Category { get; set; } = string.Empty;
    }

    public class ChecklistItem
    {
        public string Description { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public bool IsImportant { get; set; }
    }
}