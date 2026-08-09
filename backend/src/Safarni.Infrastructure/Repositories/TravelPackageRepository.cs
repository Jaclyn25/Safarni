using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Repositories;

public sealed class TravelPackageRepository : ITravelPackageRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TravelPackageRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TravelPackage?> GetByIdAsync(
        Guid packageId,
        bool includeDays = false,
        CancellationToken cancellationToken = default)
    {
        return BaseQuery(includeDays)
            .FirstOrDefaultAsync(package => package.Id == packageId, cancellationToken);
    }

    public Task<TravelPackage?> GetByIdForAgencyAsync(
        Guid packageId,
        Guid agencyId,
        bool includeDays = false,
        CancellationToken cancellationToken = default)
    {
        return BaseQuery(includeDays)
            .FirstOrDefaultAsync(
                package => package.Id == packageId && package.TravelAgencyId == agencyId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TravelPackage>> ListByAgencyAsync(
        Guid agencyId,
        CancellationToken cancellationToken = default)
    {
        return await BaseQuery(includeDays: true)
            .Where(package => package.TravelAgencyId == agencyId)
            .OrderByDescending(package => package.UpdatedAt ?? package.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<TravelPackage>> ListPublishedAsync(
        PublishedPackageFilters filters,
        CancellationToken cancellationToken = default)
    {
        var query = BaseQuery(includeDays: false)
            .AsNoTracking()
            .Where(package =>
                package.IsPublished &&
                package.IsActive &&
                package.TravelAgency.VerificationStatus == AgencyVerificationStatus.Verified);

        if (!string.IsNullOrWhiteSpace(filters.Destination))
        {
            var destination = filters.Destination.Trim();
            query = query.Where(package => package.Destination.Contains(destination));
        }

        if (filters.StartDate is { } startDate)
        {
            query = query.Where(package => package.StartDate >= startDate.Date);
        }

        if (filters.DurationDays is { } durationDays)
        {
            query = query.Where(package => package.DurationDays == durationDays);
        }

        if (filters.MinPrice is { } minPrice)
        {
            query = query.Where(package => package.Price >= minPrice);
        }

        if (filters.MaxPrice is { } maxPrice)
        {
            query = query.Where(package => package.Price <= maxPrice);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(package => package.StartDate)
            .ThenBy(package => package.Title)
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TravelPackage>(items, filters.Page, filters.PageSize, totalCount);
    }

    public Task AddAsync(TravelPackage package, CancellationToken cancellationToken = default)
    {
        return _dbContext.TravelPackages.AddAsync(package, cancellationToken).AsTask();
    }

    public void Remove(TravelPackage package)
    {
        _dbContext.TravelPackages.Remove(package);
    }

    public void RemoveDay(TravelPackageDay day)
    {
        _dbContext.TravelPackageDays.Remove(day);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<TravelPackage> BaseQuery(bool includeDays)
    {
        var query = _dbContext.TravelPackages
            .Include(package => package.TravelAgency);

        return includeDays
            ? query.Include(package => package.Days)
            : query;
    }
}
