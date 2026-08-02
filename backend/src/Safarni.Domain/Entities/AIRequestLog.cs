namespace Safarni.Domain.Entities;

public class AIRequestLog
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public Guid? ConversationId { get; set; }

    public Guid? ItineraryJobId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Operation { get; set; } = string.Empty;

    public string Status { get; set; } = "Success";

    public long LatencyMs { get; set; }

    public int? PromptTokens { get; set; }

    public int? CompletionTokens { get; set; }

    public string? ErrorCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
