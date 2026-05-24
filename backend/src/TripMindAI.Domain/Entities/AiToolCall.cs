using TripMindAI.Domain.Enums;

namespace TripMindAI.Domain.Entities;

public class AiToolCall
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Message Message { get; set; } = null!;
    public string ToolName { get; set; } = string.Empty;
    public string RequestJson { get; set; } = string.Empty;
    public string? ResponseJson { get; set; }
    public AiToolCallStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
