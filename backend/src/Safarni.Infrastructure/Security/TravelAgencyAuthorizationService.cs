using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Security;
using Safarni.Domain.Enums;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Security;

public sealed class TravelAgencyAuthorizationService
    : ITravelAgencyAuthorizationService
{
    private readonly ApplicationDbContext _dbContext;

    public TravelAgencyAuthorizationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> IsVerifiedAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TravelAgencies
            .AsNoTracking()
            .AnyAsync(
                agency => agency.UserId == userId &&
                    agency.VerificationStatus == AgencyVerificationStatus.Verified,
                cancellationToken);
    }
}
