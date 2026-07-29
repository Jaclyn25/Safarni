using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Domain.Entities;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Repositories;

internal sealed class TripRepository : ITripRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TripRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<Trip>()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task AddAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<Trip>().AddAsync(trip, cancellationToken);
    }

    public void Update(Trip trip)
    {
        _dbContext.Set<Trip>().Update(trip);
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
