using System;
using System.Collections.Generic;
using Safarni.Domain.Entities;

namespace ChatBotApi.Domain.Entities;

public class ChatFolder
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User User { get; set; } = null!;
    public List<Conversation> Conversations { get; set; } = new();
}
