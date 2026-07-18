
using Safarni.Domain.Entities;

namespace Safarni.Application.Abstractions.Persistence;

public interface IUserSettingsRepository
{
    Task<UserSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(UserSettings settings, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserSettings settings, CancellationToken cancellationToken = default);
}
