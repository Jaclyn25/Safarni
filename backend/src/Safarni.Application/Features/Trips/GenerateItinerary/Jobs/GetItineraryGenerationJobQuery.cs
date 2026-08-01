using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Trips.GenerateItinerary.Jobs;

public sealed record GetItineraryGenerationJobQuery(
    Guid TripId,
    Guid JobId)
    : IRequest<Result<ItineraryGenerationJobResponse>>;

public sealed class GetItineraryGenerationJobQueryHandler
    : IRequestHandler<
        GetItineraryGenerationJobQuery,
        Result<ItineraryGenerationJobResponse>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IItineraryGenerationJobRepository _jobs;

    public GetItineraryGenerationJobQueryHandler(
        ICurrentUserService currentUser,
        IItineraryGenerationJobRepository jobs)
    {
        _currentUser = currentUser;
        _jobs = jobs;
    }

    public async Task<Result<ItineraryGenerationJobResponse>> Handle(
        GetItineraryGenerationJobQuery request,
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

        return Result<ItineraryGenerationJobResponse>.Success(
            ItineraryGenerationJobResponseMapper.Map(job));
    }
}
