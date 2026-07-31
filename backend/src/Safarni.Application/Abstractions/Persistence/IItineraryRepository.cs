using Safarni.Domain.Entities;

namespace Safarni.Application.Abstractions.Persistence;

public interface IItineraryRepository
{
    Task<Itinerary?> GetActiveByTripIdAsync(
        Guid tripId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Itinerary itinerary,
        CancellationToken cancellationToken = default);

    void RemoveItem(ItineraryItem item);

    Task SaveAsync(CancellationToken cancellationToken = default);
}
