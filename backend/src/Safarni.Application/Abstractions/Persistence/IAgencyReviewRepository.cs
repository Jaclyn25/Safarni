using Safarni.Domain.Entities;

namespace Safarni.Application.Abstractions.Persistence;

public sealed record AgencyReviewSummary(double AverageRating, int ReviewCount);

public interface IAgencyReviewRepository
{
    Task<IReadOnlyList<AgencyReview>> ListPublishedAsync(Guid agencyId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountPublishedAsync(Guid agencyId, CancellationToken cancellationToken = default);
    Task<AgencyReviewSummary> GetSummaryAsync(Guid agencyId, CancellationToken cancellationToken = default);
    Task<AgencyReview?> GetForAgencyAsync(Guid agencyId, Guid reviewId, CancellationToken cancellationToken = default);
    Task AddAsync(AgencyReview review, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
