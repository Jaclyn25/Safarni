using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Safarni.Application.Abstractions.BackgroundJobs;
using Safarni.Application.Abstractions.Notifications;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Features.Trips.GenerateItinerary;
using Safarni.Domain.Entities;

namespace Safarni.Infrastructure.BackgroundJobs;

/// <summary>
/// Durable, database-backed itinerary worker.
///
/// The database remains the source of truth and SQL row locking prevents
/// duplicate processing across application instances. A process-local signal
/// wakes the worker immediately when this API process queues work, while a
/// slow fallback poll preserves recovery across restarts or multiple instances.
/// </summary>
internal sealed class ItineraryGenerationWorker : BackgroundService
{
    // With the signal in place there is no reason to hit SQL every 2 seconds
    // while idle. This fallback only protects cross-process/crash scenarios.
    private static readonly TimeSpan EmptyQueueFallbackPollInterval =
        TimeSpan.FromSeconds(30);

    private static readonly TimeSpan FailureDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan StaleJobAge = TimeSpan.FromMinutes(10);

    // Stale recovery is maintenance work, not queue polling. Running it once
    // per minute is enough for a 10-minute stale threshold.
    private static readonly TimeSpan StaleRecoveryInterval =
        TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IItineraryGenerationQueueSignal _queueSignal;
    private readonly ILogger<ItineraryGenerationWorker> _logger;
    private readonly string _workerId =
        $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";

    public ItineraryGenerationWorker(
        IServiceScopeFactory scopeFactory,
        IItineraryGenerationQueueSignal queueSignal,
        ILogger<ItineraryGenerationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _queueSignal = queueSignal;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var nextStaleRecoveryAtUtc = DateTime.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                if (now >= nextStaleRecoveryAtUtc)
                {
                    await RecoverStaleJobsAsync(stoppingToken);
                    nextStaleRecoveryAtUtc =
                        DateTime.UtcNow.Add(StaleRecoveryInterval);
                }

                var processed = await TryProcessNextAsync(stoppingToken);

                if (processed)
                {
                    // Drain the durable queue immediately while work exists.
                    // No delay and no extra signal are needed between jobs.
                    continue;
                }

                // Queue is empty. Sleep without touching the database.
                // A newly-created local job wakes this wait immediately.
                // The timeout is only a durable fallback for missed/cross-process
                // signals and therefore may safely be much slower than 2 seconds.
                await _queueSignal.WaitAsync(
                    EmptyQueueFallbackPollInterval,
                    stoppingToken);
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
                    "The itinerary generation worker failed while processing the queue.");

                await Task.Delay(FailureDelay, stoppingToken);
            }
        }
    }

    private async Task RecoverStaleJobsAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobs = scope.ServiceProvider
            .GetRequiredService<IItineraryGenerationJobRepository>();

        var recovered = await jobs.RequeueStaleRunningJobsAsync(
            DateTime.UtcNow.Subtract(StaleJobAge),
            cancellationToken);

        if (recovered > 0)
        {
            _logger.LogWarning(
                "Recovered {RecoveredJobCount} stale itinerary generation job(s).",
                recovered);
        }
    }

    private async Task<bool> TryProcessNextAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobs = scope.ServiceProvider
            .GetRequiredService<IItineraryGenerationJobRepository>();

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
                await NotifyItineraryReadyAsync(
                    scope,
                    job,
                    existingItineraryId,
                    cancellationToken);
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

            if (result.IsSuccess)
            {
                await NotifyItineraryReadyAsync(
                    scope,
                    job,
                    result.Value!.Id,
                    cancellationToken);
            }
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
            // healthy worker observes it after the stale threshold.
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
            await jobs.SaveAsync(cancellationToken);
        }

        return true;
    }

    private async Task NotifyItineraryReadyAsync(
        IServiceScope scope,
        ItineraryGenerationJob job,
        Guid itineraryId,
        CancellationToken cancellationToken)
    {
        try
        {
            var notifications = scope.ServiceProvider
                .GetRequiredService<ITravelNotificationService>();

            await notifications.SendItinerarySummaryAsync(
                job.TripId,
                job.UserId,
                itineraryId,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Itinerary summary notification failed for trip {TripId} job {JobId}. " +
                "Itinerary generation is unaffected.",
                job.TripId,
                job.Id);
        }
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
