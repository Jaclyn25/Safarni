using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Repositories;

public sealed class PackageBookingRequestRepository : IPackageBookingRequestRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PackageBookingRequestRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PackageBookingRequest?> GetByIdForTravelerAsync(Guid requestId, Guid travelerUserId, CancellationToken cancellationToken = default)
    {
        return BaseQuery().FirstOrDefaultAsync(request =>
            request.Id == requestId && request.TravelerUserId == travelerUserId, cancellationToken);
    }

    public Task<PackageBookingRequest?> GetByIdForAgencyAsync(Guid requestId, Guid agencyId, CancellationToken cancellationToken = default)
    {
        return BaseQuery().FirstOrDefaultAsync(request =>
            request.Id == requestId && request.TravelPackage.TravelAgencyId == agencyId, cancellationToken);
    }

    public async Task<IReadOnlyList<PackageBookingRequest>> ListByTravelerAsync(Guid travelerUserId, CancellationToken cancellationToken = default)
    {
        return await BaseQuery()
            .Where(request => request.TravelerUserId == travelerUserId)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PackageBookingRequest>> ListByAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        return await BaseQuery()
            .Where(request => request.TravelPackage.TravelAgencyId == agencyId)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetAcceptedTravelerCountAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        var count = await _dbContext.PackageBookingRequests
            .Where(request => request.TravelPackageId == packageId &&
                request.Status == PackageBookingRequestStatus.Accepted)
            .SumAsync(request => (int?)request.TravelerCount, cancellationToken);
        return count ?? 0;
    }

    public Task AddAsync(PackageBookingRequest request, CancellationToken cancellationToken = default)
    {
        return _dbContext.PackageBookingRequests.AddAsync(request, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<PackageBookingRequest> BaseQuery()
    {
        return _dbContext.PackageBookingRequests
            .Include(request => request.TravelPackage)
                .ThenInclude(package => package.TravelAgency)
            .Include(request => request.TravelerUser);
    }
}
