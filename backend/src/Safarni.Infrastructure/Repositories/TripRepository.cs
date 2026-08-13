using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Domain.Entities;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Repositories;

internal sealed class TripRepository : ITripRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TripRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Trip?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Trips
            .Include(trip => trip.Cities)
            .FirstOrDefaultAsync(
                trip => trip.Id == id,
                cancellationToken);
    }

    public Task<Trip?> GetByIdWithItinerariesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Trips
            .AsNoTracking()
            .AsSplitQuery()
            .Include(trip => trip.Cities)
            .Include(trip => trip.Itineraries
                .Where(itinerary => itinerary.IsActive))
                .ThenInclude(itinerary => itinerary.Days)
                    .ThenInclude(day => day.Items)
            .FirstOrDefaultAsync(
                trip => trip.Id == id,
                cancellationToken);
    }

    public Task<Trip?> GetByIdWithSnapshotAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Trips
            .AsSplitQuery()
            .Include(trip => trip.Cities)
            .Include(trip => trip.Itineraries
                .Where(itinerary => itinerary.IsActive))
                .ThenInclude(itinerary => itinerary.Days)
                    .ThenInclude(day => day.Items)
            .Include(trip => trip.PublicSnapshot)
                .ThenInclude(snapshot => snapshot.Versions)
            .FirstOrDefaultAsync(
                trip => trip.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Trip>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Trips
            .AsNoTracking()
            .AsSplitQuery()
            .Where(trip => trip.UserId == userId)
            .Include(trip => trip.Cities)
            .Include(trip => trip.Itineraries
                .Where(itinerary => itinerary.IsActive))
                .ThenInclude(itinerary => itinerary.Days)
                    .ThenInclude(day => day.Items)
            .OrderByDescending(
                trip => trip.UpdatedAt ?? trip.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Trip>> GetPublishedTripsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Trips
            .AsNoTracking()
            .AsSplitQuery()
            .Where(trip =>
                trip.UserId == userId &&
                trip.IsPublished)
            .Include(trip => trip.PublicSnapshot)
                .ThenInclude(snapshot => snapshot.Versions)
            .OrderByDescending(trip => trip.PublishedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Trip trip,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Trips.AddAsync(
            trip,
            cancellationToken);
    }

    public async Task AddPublicSnapshotAsync(
        PublicTripSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.PublicTripSnapshots.AddAsync(
            snapshot,
            cancellationToken);
    }

    public void Remove(Trip trip)
    {
        _dbContext.Trips.Remove(trip);
    }

    public Task SaveAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}