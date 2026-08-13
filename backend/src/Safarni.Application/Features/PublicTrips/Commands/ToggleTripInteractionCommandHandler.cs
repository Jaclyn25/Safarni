using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.PublicTrips.Commands
{
    public sealed class ToggleTripInteractionCommandHandler
        : IRequestHandler<ToggleTripInteractionCommand, Result<ToggleTripInteractionResult>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IPublicTripRepository _repository;

        public ToggleTripInteractionCommandHandler(
            ICurrentUserService currentUserService,
            IPublicTripRepository repository)
        {
            _currentUserService = currentUserService;
            _repository = repository;
        }

        public async Task<Result<ToggleTripInteractionResult>> Handle(
            ToggleTripInteractionCommand request,
            CancellationToken cancellationToken)
        {
            if (_currentUserService.UserId is not { } userId)
            {
                return Result<ToggleTripInteractionResult>.Failure(
                    AuthenticationErrors.AuthenticationRequired);
            }

            var version = await _repository.GetSnapshotVersionWithInteractionsAsync(request.VersionId, cancellationToken);

            if (version == null)
            {
                return Result<ToggleTripInteractionResult>.Failure(
                    new Error("PublicTrip.NotFound", "Public trip version not found.", ErrorType.NotFound));
            }

            var existingInteraction = version.Interactions
                .FirstOrDefault(i => i.UserId == userId && i.Type == request.InteractionType);

            bool isActive;

            if (existingInteraction != null)
            {
                // Toggle off
                _repository.RemoveTripInteraction(existingInteraction);
                
                if (request.InteractionType == TripInteractionType.Like)
                    version.LikesCount = Math.Max(0, version.LikesCount - 1);
                else if (request.InteractionType == TripInteractionType.Save)
                    version.SavesCount = Math.Max(0, version.SavesCount - 1);
                
                isActive = false;
            }
            else
            {
                // Toggle on
                var interaction = new TripInteraction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    SnapshotVersionId = version.Id,
                    Type = request.InteractionType,
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.AddTripInteractionAsync(interaction, cancellationToken);

                if (request.InteractionType == TripInteractionType.Like)
                    version.LikesCount++;
                else if (request.InteractionType == TripInteractionType.Save)
                    version.SavesCount++;

                isActive = true;
            }

            await _repository.SaveChangesAsync(cancellationToken);

            var newCount = request.InteractionType == TripInteractionType.Like
                ? version.LikesCount
                : version.SavesCount;

            return Result<ToggleTripInteractionResult>.Success(
                new ToggleTripInteractionResult(version.Id, newCount, isActive));
        }
    }
}