using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.PublicTrips.Commands
{
    public sealed class CopyPublicTripCommandHandler
        : IRequestHandler<CopyPublicTripCommand, Result<CopyPublicTripResult>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IPublicTripRepository _publicTripRepository;
        private readonly ITripRepository _tripRepository;

        public CopyPublicTripCommandHandler(
            ICurrentUserService currentUserService,
            IPublicTripRepository publicTripRepository,
            ITripRepository tripRepository)
        {
            _currentUserService = currentUserService;
            _publicTripRepository = publicTripRepository;
            _tripRepository = tripRepository;
        }

        public async Task<Result<CopyPublicTripResult>> Handle(
            CopyPublicTripCommand request,
            CancellationToken cancellationToken)
        {
            if (_currentUserService.UserId is not { } userId)
            {
                return Result<CopyPublicTripResult>.Failure(
                    AuthenticationErrors.AuthenticationRequired);
            }

            var version = await _publicTripRepository.GetSnapshotVersionWithTripAsync(request.VersionId, cancellationToken);

            if (version == null)
            {
                return Result<CopyPublicTripResult>.Failure(
                    new Error("PublicTrip.NotFound", "Public trip version not found.", ErrorType.NotFound));
            }

            if (!version.IsActive)
            {
                return Result<CopyPublicTripResult>.Failure(
                    new Error("PublicTrip.NotActive", "This snapshot version is no longer active.", ErrorType.Validation));
            }

            // Create new Trip based on snapshot
            var originalTrip = version.Snapshot.Trip;
            
            var newTrip = new Trip
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = $"{originalTrip.Name} (Copy)",
                Destination = originalTrip.Destination,
                OriginCountry = originalTrip.OriginCountry,
                OriginCountryCode = originalTrip.OriginCountryCode,
                OriginCity = originalTrip.OriginCity,
                OriginAirportCode = originalTrip.OriginAirportCode,
                OriginAirportName = originalTrip.OriginAirportName,
                Status = TripStatus.Draft,
                IsPublished = false,
                Visibility = TripVisibility.Private,
                SelectedCitiesJson = originalTrip.SelectedCitiesJson, // From snapshot or original trip? The snapshot has it in ItineraryJson, but we'll use original for simplicity if cities didn't change, or extract from ItineraryJson.
                StartDate = originalTrip.StartDate,
                EndDate = originalTrip.EndDate,
                CreatedAt = DateTime.UtcNow
            };

            // Increment copies count
            version.CopiesCount++;

            await _tripRepository.AddAsync(newTrip, cancellationToken);
            await _publicTripRepository.SaveChangesAsync(cancellationToken);

            return Result<CopyPublicTripResult>.Success(new CopyPublicTripResult(newTrip.Id));
        }
    }
}