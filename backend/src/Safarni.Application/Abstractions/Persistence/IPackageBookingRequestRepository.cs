using Safarni.Domain.Entities;

namespace Safarni.Application.Abstractions.Persistence;

public interface IPackageBookingRequestRepository
{
    Task<PackageBookingRequest?> GetByIdForTravelerAsync(Guid requestId, Guid travelerUserId, CancellationToken cancellationToken = default);
    Task<PackageBookingRequest?> GetByIdForAgencyAsync(Guid requestId, Guid agencyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PackageBookingRequest>> ListByTravelerAsync(Guid travelerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PackageBookingRequest>> ListByAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default);
    Task<int> GetAcceptedTravelerCountAsync(Guid packageId, CancellationToken cancellationToken = default);
    Task<bool> HasOpenRequestAsync(Guid packageId, Guid travelerUserId, CancellationToken cancellationToken = default);
    Task<int> CountAcceptedByAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default);
    Task<int> SumAcceptedTravelersByAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default);
    Task AddAsync(PackageBookingRequest request, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
