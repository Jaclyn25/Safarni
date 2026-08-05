namespace ChatBotApi.Application.Common.Models;

public enum AICompletionResponseFormat
{
    Text,
    ItineraryJson
}

public sealed record AICompletionOptions(
    double? Temperature = null,
    int? MaxTokens = null,
    double? TopP = null,
    string? ReasoningEffort = null,
    AICompletionResponseFormat ResponseFormat =
        AICompletionResponseFormat.Text)
{
    public static AICompletionOptions ItineraryPlanning { get; } =
        new(
            Temperature: 0.1,
            MaxTokens: 2400,
            TopP: 0.9,
            ReasoningEffort: "low",
            ResponseFormat:
                AICompletionResponseFormat.ItineraryJson);
}
