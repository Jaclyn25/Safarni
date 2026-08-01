namespace Safarni.Application.Abstractions.BackgroundJobs;

/// <summary>
/// Wakes the in-process itinerary generation worker when a new durable job is queued.
/// The database remains the source of truth; this signal only avoids unnecessary polling.
/// </summary>
public interface IItineraryGenerationQueueSignal
{
    /// <summary>
    /// Notifies the worker that queue state may have changed.
    /// Multiple notifications may be safely coalesced into one wake-up.
    /// </summary>
    void Signal();

    /// <summary>
    /// Waits until a queue signal arrives or the fallback timeout expires.
    /// Returns true when signaled and false when the timeout expires.
    /// </summary>
    Task<bool> WaitAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken);
}
