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

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}