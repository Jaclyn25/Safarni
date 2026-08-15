using Safarni.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Safarni.Application.Abstractions.Persistence;

public interface IPublicTripRepository
{
    Task<SnapshotVersion?> GetSnapshotVersionWithTripAndUserAsync(
        Guid versionId,
        CancellationToken cancellationToken = default);

    Task<SnapshotVersion?> GetSnapshotVersionWithInteractionsAsync(
        Guid versionId,
        CancellationToken cancellationToken = default);

    Task<SnapshotVersion?> GetSnapshotVersionWithRatingsAsync(
        Guid versionId,
        CancellationToken cancellationToken = default);

    Task<SnapshotVersion?> GetSnapshotVersionWithTripAsync(
        Guid versionId,
        CancellationToken cancellationToken = default);

    Task AddTripInteractionAsync(
        TripInteraction interaction,
        CancellationToken cancellationToken = default);

    void RemoveTripInteraction(
        TripInteraction interaction);

    Task AddTripRatingAsync(
        TripRating rating,
        CancellationToken cancellationToken = default);

    void RemoveTripRating(
        TripRating rating);

    Task<IReadOnlyCollection<SnapshotVersion>> SearchPublicTripsAsync(
        string? destination,
        int? minDuration,
        int? maxDuration,
        decimal? maxBudget,
        string? sortOrder,
        CancellationToken cancellationToken = default);

    Task<bool> TryRecordViewIfNotExistsAsync(
        Guid versionId, 
        Guid? userId, 
        string? sessionId, 
        TimeSpan window, 
        CancellationToken cancellationToken = default);

    Task IncrementViewsCountAsync(
        Guid versionId, 
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}