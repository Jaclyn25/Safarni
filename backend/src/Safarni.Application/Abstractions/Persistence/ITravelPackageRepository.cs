using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;

namespace Safarni.Application.Abstractions.Persistence;

public sealed record PublishedPackageFilters(
    string? Destination,
    DateTime? StartDate,
    int? DurationDays,
    decimal? MinPrice,
    decimal? MaxPrice,
    int Page,
    int PageSize);

public interface ITravelPackageRepository
{
    Task<TravelPackage?> GetByIdAsync(Guid packageId, bool includeDays = false, CancellationToken cancellationToken = default);
    Task<TravelPackage?> GetByIdForAgencyAsync(Guid packageId, Guid agencyId, bool includeDays = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TravelPackage>> ListByAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default);
    Task<PagedResult<TravelPackage>> ListPublishedAsync(PublishedPackageFilters filters, CancellationToken cancellationToken = default);
    Task AddAsync(TravelPackage package, CancellationToken cancellationToken = default);
    void Remove(TravelPackage package);

    void RemoveDay(TravelPackageDay day);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
