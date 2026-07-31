using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Models;

namespace ChatBotApi.Application.Common.Interfaces;

public interface IItineraryAIProvider
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
}

public interface IAIProvider : IItineraryAIProvider
{
    IAsyncEnumerable<string> StreamChatCompletionAsync(
        string systemPrompt,
        List<ChatMessageDto> history,
        string userMessage,
        CancellationToken cancellationToken = default);
}
