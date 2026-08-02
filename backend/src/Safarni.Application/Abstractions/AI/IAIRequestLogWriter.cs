namespace Safarni.Application.Abstractions.AI;

public sealed record AIRequestLogEntry(
    Guid? UserId,
    Guid? ConversationId,
    Guid? ItineraryJobId,
    string Provider,
    string Model,
    string Operation,
    string Status,
    long LatencyMs,
    int? PromptTokens = null,
    int? CompletionTokens = null,
    string? ErrorCode = null);

public interface IAIRequestLogWriter
{
    Task RecordAsync(
        AIRequestLogEntry entry,
        CancellationToken cancellationToken = default);
}
