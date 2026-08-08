namespace Safarni.Application.Abstractions.AI;

/// <summary>
/// Model abstraction for Safarni's itinerary agent. The application owns
/// the bounded tool loop; providers only return the model's next decision.
/// </summary>
public interface IItineraryAgentModel
{
    string ProviderName { get; }

    string ModelName { get; }

    int MaxAgentSteps { get; }

    Task<ItineraryAgentModelResponse> GetNextDecisionAsync(
        string systemPrompt,
        IReadOnlyList<ItineraryAgentMessage> messages,
        IReadOnlyList<ItineraryAgentToolDefinition> tools,
        CancellationToken cancellationToken = default);
}

public enum ItineraryAgentMessageRole
{
    User,
    Model,
    Tool
}

public sealed record ItineraryAgentMessage(
    ItineraryAgentMessageRole Role,
    string? Text = null,
    IReadOnlyList<ItineraryAgentFunctionCall>? FunctionCalls = null,
    IReadOnlyList<ItineraryAgentFunctionResult>? FunctionResults = null,
    string? RawContentJson = null);

public sealed record ItineraryAgentToolDefinition(
    string Name,
    string Description,
    string ParametersJson);

public sealed record ItineraryAgentFunctionCall(
    string Id,
    string Name,
    string ArgumentsJson,
    string? ThoughtSignature = null);

public sealed record ItineraryAgentFunctionResult(
    string Id,
    string Name,
    string ResultJson);

public sealed record ItineraryAgentTokenUsage(
    long PromptTokenCount,
    long CandidatesTokenCount,
    long ThoughtsTokenCount,
    long ToolUsePromptTokenCount,
    long CachedContentTokenCount,
    long TotalTokenCount);

public sealed record ItineraryAgentModelResponse(
    string? Text,
    IReadOnlyList<ItineraryAgentFunctionCall> FunctionCalls,
    string? RawContentJson = null,
    ItineraryAgentTokenUsage? Usage = null);
