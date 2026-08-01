using Safarni.Application.Abstractions.BackgroundJobs;

namespace Safarni.Infrastructure.BackgroundJobs;

/// <summary>
/// Process-local wake signal. The durable SQL queue is still authoritative,
/// so losing this signal during a process crash cannot lose a generation job.
/// </summary>
internal sealed class ItineraryGenerationQueueSignal
    : IItineraryGenerationQueueSignal,
      IDisposable
{
    private readonly SemaphoreSlim _signal = new(0, 1);

    public void Signal()
    {
        try
        {
            // Capacity is one because one wake-up is enough: once awake, the
            // worker drains all pending jobs before becoming idle again.
            _signal.Release();
        }
        catch (SemaphoreFullException)
        {
            // A wake-up is already pending. Coalescing duplicate signals is
            // intentional and prevents an unbounded in-memory notification queue.
        }
    }

    public Task<bool> WaitAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        return _signal.WaitAsync(timeout, cancellationToken);
    }

    public void Dispose()
    {
        _signal.Dispose();
    }
}
