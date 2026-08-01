using System.Data;
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
        return QueryWithDetails()
            .FirstOrDefaultAsync(
                itinerary => itinerary.TripId == tripId &&
                             itinerary.IsActive,
                cancellationToken);
    }

    public Task<Itinerary?> GetByIdAsync(
        Guid itineraryId,
        CancellationToken cancellationToken = default)
    {
        return QueryWithDetails()
            .FirstOrDefaultAsync(
                itinerary => itinerary.Id == itineraryId,
                cancellationToken);
    }

    public async Task AddAsync(
        Itinerary itinerary,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Itineraries.AddAsync(
            itinerary,
            cancellationToken);
    }

    public async Task ReplaceActiveAsync(
        Itinerary itinerary,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        var activeItineraries = await _dbContext.Itineraries
            .Where(existing =>
                existing.TripId == itinerary.TripId &&
                existing.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var active in activeItineraries)
        {
            active.IsActive = false;
            active.UpdatedAt = itinerary.CreatedAt;
        }

        // Persist deactivation first so the filtered unique index cannot be
        // violated by SQL command ordering in one SaveChanges batch.
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.Itineraries.AddAsync(
            itinerary,
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public void RemoveItem(ItineraryItem item)
    {
        _dbContext.ItineraryItems.Remove(item);
    }

    public Task SaveAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Itinerary> QueryWithDetails()
    {
        return _dbContext.Itineraries
            .Include(itinerary => itinerary.Days)
                .ThenInclude(day => day.Items)
            .AsSplitQuery();
    }
}
