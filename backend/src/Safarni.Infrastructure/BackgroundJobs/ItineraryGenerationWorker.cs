using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Features.Trips.GenerateItinerary;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.BackgroundJobs;

/// <summary>
/// Durable, database-backed itinerary worker. Only pending jobs are claimed,
/// and the SQL Server repository uses UPDLOCK/READPAST so multiple application
/// instances do not process the same job.
/// </summary>
internal sealed class ItineraryGenerationWorker : BackgroundService
{
    private static readonly TimeSpan EmptyQueueDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan FailureDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan StaleJobAge = TimeSpan.FromMinutes(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ItineraryGenerationWorker> _logger;
    private readonly string _workerId =
        $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";

    public ItineraryGenerationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ItineraryGenerationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await TryProcessNextAsync(stoppingToken);
                if (!processed)
                {
                    await Task.Delay(EmptyQueueDelay, stoppingToken);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "The itinerary generation worker failed while polling the queue.");
                await Task.Delay(FailureDelay, stoppingToken);
            }
        }
    }

    private async Task<bool> TryProcessNextAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobs = scope.ServiceProvider
            .GetRequiredService<IItineraryGenerationJobRepository>();

        await jobs.RequeueStaleRunningJobsAsync(
            DateTime.UtcNow.Subtract(StaleJobAge),
            cancellationToken);

        var job = await jobs.ClaimNextPendingAsync(
            _workerId,
            DateTime.UtcNow,
            cancellationToken);

        if (job is null)
        {
            return false;
        }

        var itineraryRepository = scope.ServiceProvider
            .GetRequiredService<IItineraryRepository>();

        if (job.ItineraryId is { } existingItineraryId)
        {
            var existingItinerary =
                await itineraryRepository.GetByIdAsync(
                    existingItineraryId,
                    cancellationToken);

            if (existingItinerary is not null)
            {
                job.Status = ItineraryGenerationJobStatus.Completed;
                job.IsActive = false;
                job.ProgressPercent = 100;
                job.Stage = "Completed";
                job.CompletedAt = DateTime.UtcNow;
                job.UpdatedAt = job.CompletedAt.Value;
                await jobs.SaveAsync(cancellationToken);
                return true;
            }

            job.ItineraryId = null;
            job.UpdatedAt = DateTime.UtcNow;
            await jobs.SaveAsync(cancellationToken);
        }

        var processor = scope.ServiceProvider
            .GetRequiredService<IItineraryGenerationProcessor>();

        try
        {
            var preferences = GenerateItineraryPreferencesSerializer
                .Deserialize(job.Preferences);

            var result = await processor.GenerateAsync(
                job.TripId,
                job.UserId,
                preferences,
                job.BudgetAmount,
                job.CurrencyCode,
                job.TravelerCount,
                job.Id,
                cancellationToken);

            if (!await TryReloadAsync(
                    jobs,
                    job,
                    cancellationToken))
            {
                return true;
            }

            if (job.Status == ItineraryGenerationJobStatus.Cancelled)
            {
                return true;
            }

            if (result.IsSuccess)
            {
                job.Status = ItineraryGenerationJobStatus.Completed;
                job.IsActive = false;
                job.ProgressPercent = 100;
                job.Stage = "Completed";
                job.ItineraryId = result.Value!.Id;
                job.ErrorCode = null;
                job.ErrorMessage = null;
            }
            else
            {
                job.Status = ItineraryGenerationJobStatus.Failed;
                job.IsActive = false;
                job.Stage = "Failed";
                job.ErrorCode = result.Error.Code;
                job.ErrorMessage = result.Error.Message;
            }

            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = job.CompletedAt.Value;
            await jobs.SaveAsync(cancellationToken);
        }
        catch (ItineraryGenerationCancelledException)
        {
            if (!await TryReloadAsync(
                    jobs,
                    job,
                    cancellationToken))
            {
                return true;
            }

            job.Status = ItineraryGenerationJobStatus.Cancelled;
            job.IsActive = false;
            job.Stage = "Cancelled";
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = job.CompletedAt.Value;
            await jobs.SaveAsync(cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Leave the job as Running. It will be requeued as stale when a
            // healthy worker starts again.
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Itinerary job {JobId} failed unexpectedly.",
                job.Id);

            if (!await TryReloadAsync(
                    jobs,
                    job,
                    CancellationToken.None))
            {
                return true;
            }

            job.Status = ItineraryGenerationJobStatus.Failed;
            job.IsActive = false;
            job.Stage = "Failed";
            job.ErrorCode = "Itinerary.GenerationWorkerFailed";
            job.ErrorMessage = "The itinerary worker failed unexpectedly.";
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = job.CompletedAt.Value;
            await jobs.SaveAsync(CancellationToken.None);
        }

        return true;
    }

    private static async Task<bool> TryReloadAsync(
        IItineraryGenerationJobRepository jobs,
        ItineraryGenerationJob job,
        CancellationToken cancellationToken)
    {
        return await jobs.GetByIdAsync(
            job.Id,
            cancellationToken) is not null;
    }
}
