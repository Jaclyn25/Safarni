using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Domain.Entities;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Repositories;

public sealed class UserSettingsRepository : IUserSettingsRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserSettingsRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.UserSettings.FirstOrDefaultAsync(settings => settings.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(UserSettings settings, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserSettings.AddAsync(settings, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserSettings settings, CancellationToken cancellationToken = default)
    {
        _dbContext.UserSettings.Update(settings);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
