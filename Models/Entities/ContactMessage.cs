namespace LifeHub.Models.Entities
{
    public class ContactMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int Status { get; set; } // 0: New, 1: In progress, 2: Resolved
        public string? Response { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}