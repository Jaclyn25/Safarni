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
                .ThenInclude(r => r.User)
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

    public void RemoveTripRating(TripRating rating)
    {
        _dbContext.TripRatings.Remove(rating);
    }

    public async Task<IReadOnlyCollection<SnapshotVersion>> SearchPublicTripsAsync(
        string? destination,
        int? minDuration,
        int? maxDuration,
        decimal? maxBudget,
        string? sortOrder,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SnapshotVersions
            .Include(sv => sv.Snapshot)
                .ThenInclude(s => s.Trip)
                    .ThenInclude(t => t.User)
            .Where(sv => sv.IsActive 
                && sv.Snapshot.Trip.Visibility == Domain.Enums.TripVisibility.Public
                && sv.Snapshot.Trip.Status == Domain.Enums.TripStatus.Completed);

        if (!string.IsNullOrWhiteSpace(destination))
        {
            query = query.Where(sv => sv.Snapshot.Trip.Destination.Contains(destination));
        }

        if (minDuration.HasValue)
        {
            query = query.Where(sv => EF.Functions.DateDiffDay(sv.Snapshot.Trip.StartDate, sv.Snapshot.Trip.EndDate) >= minDuration.Value);
        }

        if (maxDuration.HasValue)
        {
            query = query.Where(sv => EF.Functions.DateDiffDay(sv.Snapshot.Trip.StartDate, sv.Snapshot.Trip.EndDate) <= maxDuration.Value);
        }

        if (maxBudget.HasValue)
        {
            query = query.Where(sv => sv.CostAmount != null && sv.CostAmount <= maxBudget.Value);
        }

        query = sortOrder?.ToLower() switch
        {
            "popular" => query.OrderByDescending(sv => sv.ViewsCount),
            "top_rated" => query.OrderByDescending(sv => sv.AverageRating),
            _ => query.OrderByDescending(sv => sv.Snapshot.Trip.PublishedAt ?? sv.CreatedAt)
        };

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> TryRecordViewIfNotExistsAsync(
        Guid versionId,
        Guid? userId,
        string? sessionId,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(window);

        // Use Serializable transaction to prevent race conditions completely
        using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        var exists = await _dbContext.PublicTripViews
            .AnyAsync(v => v.SnapshotVersionId == versionId
                        && (userId != null ? v.UserId == userId : v.SessionId == sessionId)
                        && v.ViewedAt >= cutoff, cancellationToken);

        if (exists)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var view = new PublicTripView
        {
            Id = Guid.NewGuid(),
            SnapshotVersionId = versionId,
            UserId = userId,
            SessionId = sessionId,
            ViewedAt = DateTime.UtcNow
        };

        _dbContext.PublicTripViews.Add(view);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    public async Task IncrementViewsCountAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        await _dbContext.SnapshotVersions
            .Where(v => v.Id == versionId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewsCount, p => p.ViewsCount + 1), cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}