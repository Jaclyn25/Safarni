using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Domain.Entities;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Repositories;

public sealed class AgencyReviewRepository : IAgencyReviewRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AgencyReviewRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AgencyReview>> ListPublishedAsync(Guid agencyId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        await BaseQuery().AsNoTracking()
            .Where(review => review.TravelAgencyId == agencyId && review.IsPublished)
            .OrderByDescending(review => review.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<int> CountPublishedAsync(Guid agencyId, CancellationToken cancellationToken = default) =>
        _dbContext.AgencyReviews.CountAsync(review => review.TravelAgencyId == agencyId && review.IsPublished, cancellationToken);

    public async Task<AgencyReviewSummary> GetSummaryAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AgencyReviews.Where(review => review.TravelAgencyId == agencyId && review.IsPublished);
        var count = await query.CountAsync(cancellationToken);
        var average = count == 0 ? 0 : await query.AverageAsync(review => review.Rating, cancellationToken);
        return new AgencyReviewSummary(Math.Round(average, 1), count);
    }

    public Task<AgencyReview?> GetForAgencyAsync(Guid agencyId, Guid reviewId, CancellationToken cancellationToken = default) =>
        BaseQuery().FirstOrDefaultAsync(review => review.Id == reviewId && review.TravelAgencyId == agencyId, cancellationToken);

    public Task AddAsync(AgencyReview review, CancellationToken cancellationToken = default) =>
        _dbContext.AgencyReviews.AddAsync(review, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => _dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<AgencyReview> BaseQuery() => _dbContext.AgencyReviews
        .Include(review => review.TravelerUser)
        .Include(review => review.PackageBookingRequest)
            .ThenInclude(request => request.TravelPackage);
}
