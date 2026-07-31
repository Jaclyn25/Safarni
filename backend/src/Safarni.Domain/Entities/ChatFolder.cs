using System;
using System.Collections.Generic;

namespace ChatBotApi.Domain.Entities;

public class ChatFolder
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public List<Conversation> Conversations { get; set; } = new();
}
