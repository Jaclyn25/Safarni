using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary.Jobs;

public sealed record CancelItineraryGenerationCommand(
    Guid TripId,
    Guid JobId)
    : IRequest<Result<ItineraryGenerationJobResponse>>;

public sealed class CancelItineraryGenerationCommandHandler
    : IRequestHandler<
        CancelItineraryGenerationCommand,
        Result<ItineraryGenerationJobResponse>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IItineraryGenerationJobRepository _jobs;

    public CancelItineraryGenerationCommandHandler(
        ICurrentUserService currentUser,
        IItineraryGenerationJobRepository jobs)
    {
        _currentUser = currentUser;
        _jobs = jobs;
    }

    public async Task<Result<ItineraryGenerationJobResponse>> Handle(
        CancelItineraryGenerationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<ItineraryGenerationJobResponse>.Failure(
                AuthenticationErrors.AuthenticationRequired);
        }

        var job = await _jobs.GetByIdAsync(
            request.JobId,
            cancellationToken);

        if (job is null || job.TripId != request.TripId)
        {
            return Result<ItineraryGenerationJobResponse>.Failure(
                ItineraryErrors.GenerationJobNotFound);
        }

        if (job.UserId != userId)
        {
            return Result<ItineraryGenerationJobResponse>.Failure(
                ItineraryErrors.Forbidden);
        }

        if (job.Status is ItineraryGenerationJobStatus.Pending or
            ItineraryGenerationJobStatus.Running)
        {
            job.Status = ItineraryGenerationJobStatus.Cancelled;
            job.IsActive = false;
            job.Stage = "Cancellation requested";
            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = job.CompletedAt.Value;
            await _jobs.SaveAsync(cancellationToken);
        }

        return Result<ItineraryGenerationJobResponse>.Success(
            ItineraryGenerationJobResponseMapper.Map(job));
    }
}
