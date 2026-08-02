namespace Safarni.Application.Abstractions.AI;

public static class AIRequestTelemetry
{
    /// <summary>
    /// Records an AI request log entry. Telemetry failures are swallowed so
    /// they can never break the chat or itinerary generation flow.
    /// </summary>
    public static async Task RecordAsync(
        IAIRequestLogWriter? writer,
        AIRequestLogEntry entry,
        CancellationToken cancellationToken = default)
    {
        if (writer is null)
        {
            return;
        }

        try
        {
            await writer.RecordAsync(entry, cancellationToken);
        }
        catch
        {
            // Intentionally ignored.
        }
    }
}
