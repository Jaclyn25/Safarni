using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Domain.Entities;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken);
    }

    public Task<bool> TravelAgencyCompanyNameExistsAsync(
        string companyName,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TravelAgencies.AnyAsync(
            agency => agency.CompanyName == companyName,
            cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AgencyRegistrationPersistenceResult>
        TryAddTravelAgencyAsync(
            User user,
            TravelAgency travelAgency,
            CancellationToken cancellationToken = default)
    {
        user.TravelAgency = travelAgency;
        travelAgency.User = user;

        await _dbContext.Users.AddAsync(user, cancellationToken);

        try
        {
            // A single SaveChanges call is transactional. Either both rows are
            // committed or neither row is created.
            await _dbContext.SaveChangesAsync(cancellationToken);
            return AgencyRegistrationPersistenceResult.Created;
        }
        catch (DbUpdateException)
        {
            // The pre-checks improve the response in the normal case. These
            // queries resolve concurrent registration races safely after EF's
            // failed transaction has rolled back.
            _dbContext.ChangeTracker.Clear();

            if (await EmailExistsAsync(user.Email, cancellationToken))
            {
                return AgencyRegistrationPersistenceResult.DuplicateEmail;
            }

            if (await TravelAgencyCompanyNameExistsAsync(
                    travelAgency.CompanyName,
                    cancellationToken))
            {
                return AgencyRegistrationPersistenceResult.DuplicateCompanyName;
            }

            throw;
        }
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
