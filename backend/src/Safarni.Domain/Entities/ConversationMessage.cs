using System;

namespace ChatBotApi.Domain.Entities;

public class ConversationMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Role { get; set; } = string.Empty; // 'user' or 'assistant' or 'system'
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Conversation? Conversation { get; set; }
}
