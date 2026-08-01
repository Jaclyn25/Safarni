using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Domain.Entities;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Repositories;

internal sealed class ItineraryGenerationJobRepository
    : IItineraryGenerationJobRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ItineraryGenerationJobRepository(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ItineraryGenerationJob?> GetByIdAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var tracked = _dbContext.ItineraryGenerationJobs.Local
            .FirstOrDefault(job => job.Id == jobId);

        if (tracked is not null)
        {
            await _dbContext.Entry(tracked)
                .ReloadAsync(cancellationToken);
            return tracked;
        }

        return await _dbContext.ItineraryGenerationJobs
            .FirstOrDefaultAsync(job => job.Id == jobId, cancellationToken);
    }

    public Task<ItineraryGenerationJob?> FindByIdempotencyKeyAsync(
        Guid userId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ItineraryGenerationJobs
            .FirstOrDefaultAsync(
                job => job.UserId == userId &&
                       job.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public Task<bool> HasActiveJobAsync(
        Guid tripId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ItineraryGenerationJobs.AnyAsync(
            job => job.TripId == tripId && job.IsActive,
            cancellationToken);
    }

    public async Task<bool> TryAddAsync(
        ItineraryGenerationJob job,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.ItineraryGenerationJobs.AddAsync(
            job,
            cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (IsUniqueConstraintViolation(exception))
        {
            _dbContext.Entry(job).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<ItineraryGenerationJob?> ClaimNextPendingAsync(
        string workerId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        var job = await _dbContext.ItineraryGenerationJobs
            .FromSqlRaw(
                """
                SELECT TOP (1) *
                FROM [ItineraryGenerationJobs] WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE [Status] = 'Pending' AND [IsActive] = 1
                ORDER BY [CreatedAt]
                """)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        job.Status = ItineraryGenerationJobStatus.Running;
        job.ProgressPercent = Math.Max(job.ProgressPercent, 1);
        job.Stage = "Starting itinerary generation";
        job.StartedAt ??= now;
        job.UpdatedAt = now;
        job.WorkerId = workerId;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return job;
    }

    public Task<int> RequeueStaleRunningJobsAsync(
        DateTime staleBefore,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ItineraryGenerationJobs
            .Where(job =>
                job.IsActive &&
                job.Status == ItineraryGenerationJobStatus.Running &&
                job.UpdatedAt < staleBefore)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        job => job.Status,
                        ItineraryGenerationJobStatus.Pending)
                    .SetProperty(job => job.Stage, "Queued after worker recovery")
                    .SetProperty(job => job.WorkerId, (string?)null)
                    .SetProperty(job => job.UpdatedAt, DateTime.UtcNow),
                cancellationToken);
    }

    public Task SaveAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsUniqueConstraintViolation(
        DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException &&
               sqlException.Number is 2601 or 2627;
    }
}
