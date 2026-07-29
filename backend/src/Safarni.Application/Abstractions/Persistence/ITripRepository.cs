using Safarni.Domain.Entities;

namespace Safarni.Application.Abstractions.Persistence;

public interface ITripRepository
{
    Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Trip trip, CancellationToken cancellationToken = default);
    void Update(Trip trip);
    Task SaveAsync(CancellationToken cancellationToken = default);
}
