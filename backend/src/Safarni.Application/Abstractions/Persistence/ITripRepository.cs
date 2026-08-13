using Safarni.Domain.Entities;

namespace Safarni.Application.Abstractions.Persistence;

public interface ITripRepository
{
    Task<Trip?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Trip?> GetByIdWithItinerariesAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Trip?> GetByIdWithSnapshotAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Trip>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Trip>> GetPublishedTripsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Trip trip,
        CancellationToken cancellationToken = default);

    Task AddPublicSnapshotAsync(
        PublicTripSnapshot snapshot,
        CancellationToken cancellationToken = default);

    void Remove(Trip trip);

    Task SaveAsync(
        CancellationToken cancellationToken = default);
}