using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Domain.Entities;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Repositories;

public sealed class VisaApplicationRepository : IVisaApplicationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public VisaApplicationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(VisaApplication visaApplication, CancellationToken cancellationToken = default)
    {
        await _dbContext.VisaApplications.AddAsync(visaApplication, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<VisaApplication?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.VisaApplications.FirstOrDefaultAsync(
            application => application.Id == id && application.UserId == userId,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<VisaApplication>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.VisaApplications
            .Where(application => application.UserId == userId)
            .OrderByDescending(application => application.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
