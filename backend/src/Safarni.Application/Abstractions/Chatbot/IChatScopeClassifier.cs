using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChatBotApi.Application.Common.DTOs;

namespace ChatBotApi.Application.Common.Interfaces;

public interface IChatScopeClassifier
{
    Task<ChatIntent> ClassifyAsync(
        string userMessage,
        List<ChatMessageDto> history,
        CancellationToken cancellationToken = default);
}
