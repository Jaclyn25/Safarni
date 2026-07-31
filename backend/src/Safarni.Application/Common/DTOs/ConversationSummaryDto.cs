using System;

namespace ChatBotApi.Application.Common.DTOs;

public class ConversationSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsPinned { get; set; }
    public Guid? FolderId { get; set; }
}
