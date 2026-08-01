using Safarni.Domain.Entities;

namespace Safarni.Application.Abstractions.Persistence;

public interface IItineraryRepository
{
    Task<Itinerary?> GetActiveByTripIdAsync(
        Guid tripId,
        CancellationToken cancellationToken = default);

    Task<Itinerary?> GetByIdAsync(
        Guid itineraryId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Itinerary itinerary,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically deactivates the old itinerary and activates the replacement.
    /// The implementation uses a serializable transaction together with the
    /// filtered unique index on TripId/IsActive.
    /// </summary>
    Task ReplaceActiveAsync(
        Itinerary itinerary,
        CancellationToken cancellationToken = default);

    void RemoveItem(ItineraryItem item);

    Task SaveAsync(CancellationToken cancellationToken = default);
}
