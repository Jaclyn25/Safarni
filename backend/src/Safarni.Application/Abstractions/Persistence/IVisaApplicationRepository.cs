using Safarni.Domain.Entities;

namespace Safarni.Application.Abstractions.Persistence;

public interface IVisaApplicationRepository
{
    Task AddAsync(VisaApplication visaApplication, CancellationToken cancellationToken = default);

    Task<VisaApplication?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<VisaApplication>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);
}
