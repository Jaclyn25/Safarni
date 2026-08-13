using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Trips.PublishTrip
{
    public sealed class GetPublishedTripsQueryHandler
        : IRequestHandler<GetPublishedTripsQuery, Result<IReadOnlyCollection<PublishedTripResponse>>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITripRepository _tripRepository;

        public GetPublishedTripsQueryHandler(
            ICurrentUserService currentUserService,
            ITripRepository tripRepository)
        {
            _currentUserService = currentUserService;
            _tripRepository = tripRepository;
        }

        public async Task<Result<IReadOnlyCollection<PublishedTripResponse>>> Handle(
            GetPublishedTripsQuery request,
            CancellationToken cancellationToken)
        {
            if (_currentUserService.UserId is not { } userId)
            {
                return Result<IReadOnlyCollection<PublishedTripResponse>>.Failure(
                    AuthenticationErrors.AuthenticationRequired);
            }

            var trips = await _tripRepository.GetPublishedTripsAsync(userId, cancellationToken);
            
            var responses = trips.Select(t =>
            {
                var activeVersion = t.PublicSnapshot?.Versions
                    .FirstOrDefault(version => version.IsActive);

                return new PublishedTripResponse(
                    t.Id,
                    t.Name,
                    t.Destination,
                    t.StartDate,
                    t.EndDate,
                    t.Status.ToString(),
                    t.IsPublished,
                    t.PublishedAt,
                    t.Visibility.ToString(),
                    activeVersion?.Id,
                    activeVersion?.ViewsCount ?? 0,
                    activeVersion?.LikesCount ?? 0,
                    activeVersion?.SavesCount ?? 0,
                    activeVersion?.CopiesCount ?? 0
                );
            }).ToList();

            return Result<IReadOnlyCollection<PublishedTripResponse>>.Success(responses);
        }
    }
}