using Safarni.Domain.Entities;

namespace Safarni.Application.Abstractions.Persistence;

public interface IItineraryGenerationJobRepository
{
    Task<ItineraryGenerationJob?> GetByIdAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<ItineraryGenerationJob?> FindByIdempotencyKeyAsync(
        Guid userId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveJobAsync(
        Guid tripId,
        CancellationToken cancellationToken = default);

    Task<bool> TryAddAsync(
        ItineraryGenerationJob job,
        CancellationToken cancellationToken = default);

    Task<ItineraryGenerationJob?> ClaimNextPendingAsync(
        string workerId,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task<int> RequeueStaleRunningJobsAsync(
        DateTime staleBefore,
        CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);
}
