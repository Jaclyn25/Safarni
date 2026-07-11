using Safarni.Domain.Entities;

namespace Safarni.Application.Abstractions.Persistence;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
