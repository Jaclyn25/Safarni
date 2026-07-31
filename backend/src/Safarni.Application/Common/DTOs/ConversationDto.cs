using System;
using System.Collections.Generic;

namespace ChatBotApi.Application.Common.DTOs;

public class ConversationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsPinned { get; set; }
    public Guid? FolderId { get; set; }
    public List<ChatMessageDto> Messages { get; set; } = new();
}
