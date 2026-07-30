using ChatBotApi.Application.Common.DTOs;

namespace ChatBotApi.Application.Common.Interfaces;

public sealed record AICompletionOptions(
    string? ReasoningEffort = null,
    int? MaxTokens = null)
{
    public static AICompletionOptions LowReasoning { get; } =
        new("low");

    public static AICompletionOptions ItineraryPlanning { get; } =
        new(
            ReasoningEffort: "low",
            MaxTokens: 2400);
}

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
