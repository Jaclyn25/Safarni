using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Models;

namespace ChatBotApi.Application.Common.Interfaces;

public interface IAIProvider
{
    Task<string> GetChatCompletionAsync(
        string systemPrompt,
        List<ChatMessageDto> history,
        string userMessage,
        CancellationToken cancellationToken = default);

    Task<string> GetChatCompletionAsync(
        string systemPrompt,
        List<ChatMessageDto> history,
        string userMessage,
        AICompletionOptions options,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamChatCompletionAsync(
        string systemPrompt,
        List<ChatMessageDto> history,
        string userMessage,
        CancellationToken cancellationToken = default);
}
