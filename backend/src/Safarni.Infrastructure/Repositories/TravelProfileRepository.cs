using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Domain.Entities;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Repositories;

public sealed class TravelProfileRepository : ITravelProfileRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TravelProfileRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TravelPreference?> GetTravelPreferenceByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TravelPreferences.FirstOrDefaultAsync(preference => preference.UserId == userId, cancellationToken);
    }

    public Task<PassportInfo?> GetPassportInfoByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.PassportInfos.FirstOrDefaultAsync(passportInfo => passportInfo.UserId == userId, cancellationToken);
    }

    public async Task AddTravelPreferenceAsync(TravelPreference travelPreference, CancellationToken cancellationToken = default)
    {
        await _dbContext.TravelPreferences.AddAsync(travelPreference, cancellationToken);
    }

    public async Task AddPassportInfoAsync(PassportInfo passportInfo, CancellationToken cancellationToken = default)
    {
        await _dbContext.PassportInfos.AddAsync(passportInfo, cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
