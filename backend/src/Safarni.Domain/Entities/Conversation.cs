using System;
using System.Collections.Generic;

namespace ChatBotApi.Domain.Entities;

public class Conversation
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsPinned { get; set; } = false;
    public Guid? FolderId { get; set; }
    
    // Navigation property
    public ChatFolder? Folder { get; set; }
    public List<ConversationMessage> Messages { get; set; } = new();
}
