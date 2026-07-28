using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;

namespace ChatBotApi.Application.Common.Interfaces;

public interface IAIProvider
{
    Task<string> GetChatCompletionAsync(string systemPrompt, List<ChatMessageDto> history, string userMessage, CancellationToken cancellationToken = default);
}
