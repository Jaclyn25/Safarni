using TripMindAI.Domain.Enums;

namespace TripMindAI.Domain.Entities;

public class Conversation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string Title { get; set; } = "New Travel Chat";
    public ConversationPurpose Purpose { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<TripPlan> TripPlans { get; set; } = new List<TripPlan>();
}
