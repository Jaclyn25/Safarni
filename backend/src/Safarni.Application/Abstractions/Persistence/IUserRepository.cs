using Safarni.Domain.Entities;

namespace Safarni.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> TravelAgencyCompanyNameExistsAsync(
        string companyName,
        CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task<AgencyRegistrationPersistenceResult> TryAddTravelAgencyAsync(
        User user,
        TravelAgency travelAgency,
        CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpdateAsync(User user, CancellationToken cancellationToken = default);


}
