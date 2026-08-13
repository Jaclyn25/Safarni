using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.PublicTrips.Commands
{
    public sealed class ReportPublicTripCommandHandler
        : IRequestHandler<ReportPublicTripCommand, Result<ReportPublicTripResult>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IPublicTripRepository _repository;

        public ReportPublicTripCommandHandler(
            ICurrentUserService currentUserService,
            IPublicTripRepository repository)
        {
            _currentUserService = currentUserService;
            _repository = repository;
        }

        public async Task<Result<ReportPublicTripResult>> Handle(
            ReportPublicTripCommand request,
            CancellationToken cancellationToken)
        {
            if (_currentUserService.UserId is not { } userId)
            {
                return Result<ReportPublicTripResult>.Failure(
                    AuthenticationErrors.AuthenticationRequired);
            }

            var version = await _repository.GetSnapshotVersionWithInteractionsAsync(request.VersionId, cancellationToken);

            if (version == null)
            {
                return Result<ReportPublicTripResult>.Failure(
                    new Error("PublicTrip.NotFound", "Public trip version not found.", ErrorType.NotFound));
            }

            var existingReport = version.Interactions
                .FirstOrDefault(i => i.UserId == userId && i.Type == TripInteractionType.Report);

            if (existingReport != null)
            {
                return Result<ReportPublicTripResult>.Failure(
                    new Error("PublicTrip.AlreadyReported", "You have already reported this trip.", ErrorType.Conflict));
            }

            var report = new TripInteraction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SnapshotVersionId = version.Id,
                Type = TripInteractionType.Report,
                Reason = request.Reason,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddTripInteractionAsync(report, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return Result<ReportPublicTripResult>.Success(new ReportPublicTripResult(version.Id, true));
        }
    }
}