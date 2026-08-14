using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Domain.Entities;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Repositories;

internal sealed class PublicTripRepository : IPublicTripRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PublicTripRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SnapshotVersion?> GetSnapshotVersionWithTripAndUserAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        return _dbContext.SnapshotVersions
            .Include(sv => sv.Snapshot)
                .ThenInclude(s => s.Trip)
                    .ThenInclude(t => t.User)
            .Include(sv => sv.Interactions)
            .Include(sv => sv.Ratings)
            .FirstOrDefaultAsync(sv => sv.Id == versionId, cancellationToken);
    }

    public Task<SnapshotVersion?> GetSnapshotVersionWithInteractionsAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        return _dbContext.SnapshotVersions
            .Include(sv => sv.Interactions)
            .FirstOrDefaultAsync(sv => sv.Id == versionId, cancellationToken);
    }

    public Task<SnapshotVersion?> GetSnapshotVersionWithRatingsAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        return _dbContext.SnapshotVersions
            .Include(sv => sv.Ratings)
            .FirstOrDefaultAsync(sv => sv.Id == versionId, cancellationToken);
    }

    public Task<SnapshotVersion?> GetSnapshotVersionWithTripAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        return _dbContext.SnapshotVersions
            .Include(sv => sv.Snapshot)
                .ThenInclude(s => s.Trip)
            .FirstOrDefaultAsync(sv => sv.Id == versionId, cancellationToken);
    }


    public async Task AddTripInteractionAsync(TripInteraction interaction, CancellationToken cancellationToken = default)
    {
        await _dbContext.TripInteractions.AddAsync(interaction, cancellationToken);
    }

    public void RemoveTripInteraction(TripInteraction interaction)
    {
        _dbContext.TripInteractions.Remove(interaction);
    }

    public async Task AddTripRatingAsync(TripRating rating, CancellationToken cancellationToken = default)
    {
        await _dbContext.TripRatings.AddAsync(rating, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}