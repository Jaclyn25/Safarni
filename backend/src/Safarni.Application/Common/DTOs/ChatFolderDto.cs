using System;
using System.Collections.Generic;

namespace ChatBotApi.Application.Common.DTOs;

public class ChatFolderDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ChatFolderWithConversationsDto : ChatFolderDto
{
    public List<ConversationSummaryDto> Conversations { get; set; } = new();
}
