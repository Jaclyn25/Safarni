using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.BackgroundJobs;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Abstractions.Security;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary.Jobs;

public sealed record StartItineraryGenerationCommand(
    Guid TripId,
    GenerateItineraryPreferences Preferences,
    decimal? BudgetAmount,
    string CurrencyCode,
    int TravelerCount,
    string? IdempotencyKey)
    : IRequest<Result<ItineraryGenerationJobResponse>>;

public sealed class StartItineraryGenerationCommandHandler
    : IRequestHandler<
        StartItineraryGenerationCommand,
        Result<ItineraryGenerationJobResponse>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITripRepository _trips;
    private readonly IItineraryGenerationJobRepository _jobs;
    private readonly IItineraryGenerationQueueSignal? _queueSignal;
    private readonly IEntitlementService? _entitlements;

    public StartItineraryGenerationCommandHandler(
        ICurrentUserService currentUser,
        ITripRepository trips,
        IItineraryGenerationJobRepository jobs,
        IItineraryGenerationQueueSignal? queueSignal = null,
        IEntitlementService? entitlements = null)
    {
        _currentUser = currentUser;
        _trips = trips;
        _jobs = jobs;
        _queueSignal = queueSignal;
        _entitlements = entitlements;
    }

    public async Task<Result<ItineraryGenerationJobResponse>> Handle(
        StartItineraryGenerationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<ItineraryGenerationJobResponse>.Failure(
                AuthenticationErrors.AuthenticationRequired);
        }

        var normalizedIdempotencyKey = string.IsNullOrWhiteSpace(
            request.IdempotencyKey)
            ? null
            : request.IdempotencyKey.Trim();

        if (normalizedIdempotencyKey is not null)
        {
            var existing = await _jobs.FindByIdempotencyKeyAsync(
                userId,
                normalizedIdempotencyKey,
                cancellationToken);

            if (existing is not null)
            {
                if (existing.TripId != request.TripId)
                {
                    return Result<ItineraryGenerationJobResponse>.Failure(
                        ItineraryErrors.IdempotencyKeyReused);
                }

                WakeWorkerIfPending(existing);

                return Result<ItineraryGenerationJobResponse>.Success(
                    ItineraryGenerationJobResponseMapper.Map(existing));
            }
        }

        var trip = await _trips.GetByIdAsync(
            request.TripId,
            cancellationToken);

        if (trip is null)
        {
            return Result<ItineraryGenerationJobResponse>.Failure(
                ItineraryErrors.TripNotFound);
        }

        if (trip.UserId != userId)
        {
            return Result<ItineraryGenerationJobResponse>.Failure(
                ItineraryErrors.Forbidden);
        }

        if (_entitlements is not null && !await _entitlements.HasTravelerPremiumAiAsync(userId, cancellationToken))
        {
            return Result<ItineraryGenerationJobResponse>.Failure(BillingErrors.PremiumAiSubscriptionRequired);
        }

        if (_entitlements is not null && !(await _entitlements.GetTravelerAiUsageAsync(userId, cancellationToken)).IsAllowed)
        {
            return Result<ItineraryGenerationJobResponse>.Failure(BillingErrors.TravelerAiLimitReached);
        }

        try
        {
            if (TripRouteSerializer.ReadFromTrip(trip).Count == 0)
            {
                return Result<ItineraryGenerationJobResponse>.Failure(
                    TripErrors.CorruptRoute);
            }
        }
        catch (InvalidOperationException)
        {
            return Result<ItineraryGenerationJobResponse>.Failure(
                TripErrors.CorruptRoute);
        }

        if (await _jobs.HasActiveJobAsync(
                request.TripId,
                cancellationToken))
        {
            return Result<ItineraryGenerationJobResponse>.Failure(
                ItineraryErrors.GenerationAlreadyRunning);
        }

        var now = DateTime.UtcNow;
        var job = new ItineraryGenerationJob
        {
            Id = Guid.NewGuid(),
            TripId = request.TripId,
            UserId = userId,
            Preferences = GenerateItineraryPreferencesSerializer.Serialize(
                request.Preferences),
            BudgetAmount = request.BudgetAmount,
            CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
            TravelerCount = request.TravelerCount,
            IdempotencyKey = normalizedIdempotencyKey,
            Status = ItineraryGenerationJobStatus.Pending,
            IsActive = true,
            ProgressPercent = 0,
            Stage = "Queued",
            CreatedAt = now,
            UpdatedAt = now
        };

        var added = await _jobs.TryAddAsync(job, cancellationToken);

        if (!added)
        {
            if (normalizedIdempotencyKey is not null)
            {
                var existing = await _jobs.FindByIdempotencyKeyAsync(
                    userId,
                    normalizedIdempotencyKey,
                    cancellationToken);

                if (existing is not null)
                {
                    if (existing.TripId != request.TripId)
                    {
                        return Result<ItineraryGenerationJobResponse>.Failure(
                            ItineraryErrors.IdempotencyKeyReused);
                    }

                    WakeWorkerIfPending(existing);

                    return Result<ItineraryGenerationJobResponse>.Success(
                        ItineraryGenerationJobResponseMapper.Map(existing));
                }
            }

            return Result<ItineraryGenerationJobResponse>.Failure(
                ItineraryErrors.GenerationAlreadyRunning);
        }

        // The durable row has been committed. Wake the local worker now instead
        // of waiting for another database-poll interval.
        _queueSignal?.Signal();

        return Result<ItineraryGenerationJobResponse>.Success(
            ItineraryGenerationJobResponseMapper.Map(job));
    }

    private void WakeWorkerIfPending(ItineraryGenerationJob job)
    {
        if (job.IsActive &&
            job.Status == ItineraryGenerationJobStatus.Pending)
        {
            _queueSignal?.Signal();
        }
    }
}
