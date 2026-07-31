using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Domain.Entities;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Repositories;

internal sealed class ItineraryRepository : IItineraryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ItineraryRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Itinerary?> GetActiveByTripIdAsync(
        Guid tripId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<Itinerary>()
            .Include(i => i.Days)
                .ThenInclude(d => d.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                i => i.TripId == tripId && i.IsActive,
                cancellationToken);
    }

    public async Task AddAsync(
        Itinerary itinerary,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<Itinerary>()
            .AddAsync(itinerary, cancellationToken);
    }

    public void RemoveItem(ItineraryItem item)
    {
        _dbContext.Set<ItineraryItem>().Remove(item);
    }

    public Task SaveAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
