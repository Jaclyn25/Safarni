using System.Text.Json;
using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Trips;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Trips.PublishTrip
{
    public sealed class PublishTripCommandHandler
        : IRequestHandler<PublishTripCommand, Result<PublishTripResult>>
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        private readonly ICurrentUserService _currentUserService;
        private readonly ITripRepository _tripRepository;

        public PublishTripCommandHandler(
            ICurrentUserService currentUserService,
            ITripRepository tripRepository)
        {
            _currentUserService = currentUserService;
            _tripRepository = tripRepository;
        }

        public async Task<Result<PublishTripResult>> Handle(
            PublishTripCommand request,
            CancellationToken cancellationToken)
        {
            if (_currentUserService.UserId is not { } userId)
            {
                return Result<PublishTripResult>.Failure(
                    AuthenticationErrors.AuthenticationRequired);
            }

            var trip =
                await _tripRepository.GetByIdWithSnapshotAsync(
                    request.TripId,
                    cancellationToken);

            if (trip is null)
            {
                return Result<PublishTripResult>.Failure(
                    TripErrors.NotFound);
            }

            if (trip.UserId != userId)
            {
                return Result<PublishTripResult>.Failure(
                    TripErrors.Forbidden);
            }

            if (trip.Status != TripStatus.Completed)
            {
                return Result<PublishTripResult>.Failure(
                    TripErrors.NotCompleted);
            }

            if (!request.ConfirmedNoPersonalInfo)
            {
                return Result<PublishTripResult>.Failure(
                    TripErrors.PersonalInfoNotConfirmed);
            }

            var activeItinerary =
                trip.Itineraries.FirstOrDefault(
                    itinerary => itinerary.IsActive);

            if (activeItinerary is null ||
                activeItinerary.Days.Count == 0)
            {
                return Result<PublishTripResult>.Failure(
                    TripErrors.NotCompleted);
            }

            IReadOnlyList<TripCitySelection> route;

            try
            {
                route = TripRouteSerializer.ReadFromTrip(trip);
            }
            catch (InvalidOperationException exception)
            {
                return Result<PublishTripResult>.Failure(
                    new Error(
                        "Trip.CorruptRoute",
                        exception.Message,
                        ErrorType.Conflict));
            }

            var now = DateTime.UtcNow;

            PublicTripSnapshot snapshot;
            var nextVersionNumber = 1;

            if (trip.PublicSnapshot != null)
            {
                snapshot = trip.PublicSnapshot;

                var activeVersion =
                    snapshot.Versions.FirstOrDefault(
                        version => version.IsActive);

                if (activeVersion != null)
                {
                    activeVersion.IsActive = false;

                    nextVersionNumber =
                        activeVersion.VersionNumber + 1;
                }
                else
                {
                    var maxVersion =
                        snapshot.Versions.Any()
                            ? snapshot.Versions.Max(
                                version => version.VersionNumber)
                            : 0;

                    nextVersionNumber = maxVersion + 1;
                }
            }
            else
            {
                snapshot = new PublicTripSnapshot
                {
                    Id = Guid.NewGuid(),
                    TripId = trip.Id,
                    CreatedAt = now
                };

                trip.PublicSnapshot = snapshot;

                // Explicitly register the snapshot as a new entity.
                // Without this, EF was treating it as Modified and
                // trying to UPDATE a row that did not exist.
                await _tripRepository.AddPublicSnapshotAsync(
                    snapshot,
                    cancellationToken);
            }

            var version = new SnapshotVersion
            {
                Id = Guid.NewGuid(),
                SnapshotId = snapshot.Id,
                VersionNumber = nextVersionNumber,

                ItineraryJson = JsonSerializer.Serialize(
                    BuildSnapshotPayload(
                        trip,
                        activeItinerary,
                        route),
                    JsonOptions),

                CostShown = request.CostShown,

                CostAmount =
                    request.CostShown
                        ? request.CostAmount
                        : null,

                IncludedPhotoIdsJson =
                    JsonSerializer.Serialize(
                        request.IncludedPhotoUrls ?? [],
                        JsonOptions),

                IsActive = true,
                CreatedAt = now
            };

            snapshot.Versions.Add(version);

            // Explicitly register the snapshot version as a new entity.
            // Without this, EF treats it as Modified and tries to UPDATE
            // a row that does not exist, throwing a DbUpdateConcurrencyException.
            await _tripRepository.AddSnapshotVersionAsync(
                version,
                cancellationToken);

            trip.IsPublished = true;

            trip.PublishedAt =
                trip.PublishedAt ?? now;

            trip.Visibility =
                request.Visibility;

            await _tripRepository.SaveAsync(
                cancellationToken);

            return Result<PublishTripResult>.Success(
                new PublishTripResult(
                    trip.Id,
                    snapshot.Id,
                    version.Id,
                    version.VersionNumber,
                    now));
        }

        private static PublishedTripSnapshotPayload BuildSnapshotPayload(
            Trip trip,
            Itinerary activeItinerary,
            IReadOnlyList<TripCitySelection> route)
        {
            var orderedDays = activeItinerary.Days
                .OrderBy(day => day.DayNumber)
                .Select(day =>
                    new PublishedTripDayPayload(
                        day.DayNumber,
                        day.Date,
                        day.Title,
                        day.Notes,

                        day.Items
                            .OrderBy(item => item.OrderIndex)
                            .Select(item =>
                                new PublishedTripPlacePayload(
                                    item.Id,
                                    item.Title,
                                    item.Category
                                        .ToString()
                                        .ToLowerInvariant(),
                                    item.Address,
                                    FormatTime(
                                        item.StartTime,
                                        item.EndTime),
                                    FormatDuration(item),
                                    item.Description,
                                    item.EstimatedCost,
                                    item.PhotoUrl,
                                    false))
                            .ToList()))
                .ToList();

            return new PublishedTripSnapshotPayload(
                trip.Id,
                trip.Name,
                trip.Destination,
                trip.StartDate,
                trip.EndDate,
                route,
                orderedDays);
        }

        private static string? FormatTime(
            TimeSpan? startTime,
            TimeSpan? endTime)
        {
            if (startTime is null &&
                endTime is null)
            {
                return null;
            }

            if (startTime is not null &&
                endTime is not null)
            {
                return
                    $"{startTime:hh\\:mm} - {endTime:hh\\:mm}";
            }

            return startTime is not null
                ? startTime.Value.ToString(@"hh\:mm")
                : endTime!.Value.ToString(@"hh\:mm");
        }

        private static string? FormatDuration(
            ItineraryItem item)
        {
            if (item.StartTime is null ||
                item.EndTime is null)
            {
                return item.RouteDurationMinutes is > 0
                    ? FormatDurationMinutes(
                        item.RouteDurationMinutes.Value)
                    : null;
            }

            var duration =
                item.EndTime.Value -
                item.StartTime.Value;

            if (duration < TimeSpan.Zero)
            {
                return null;
            }

            return FormatDurationMinutes(
                (int)Math.Round(
                    duration.TotalMinutes));
        }

        private static string FormatDurationMinutes(
            int totalMinutes)
        {
            if (totalMinutes <= 0)
            {
                return string.Empty;
            }

            var parts = new List<string>();

            var hours = totalMinutes / 60;
            var minutes = totalMinutes % 60;

            if (hours > 0)
            {
                parts.Add($"{hours}h");
            }

            if (minutes > 0)
            {
                parts.Add($"{minutes}m");
            }

            return string.Join(" ", parts);
        }

        private sealed record PublishedTripSnapshotPayload(
            Guid TripId,
            string TripName,
            string Destination,
            DateTime StartDate,
            DateTime EndDate,
            IReadOnlyList<TripCitySelection> Cities,
            IReadOnlyList<PublishedTripDayPayload> Days);

        private sealed record PublishedTripDayPayload(
            int DayNumber,
            DateTime Date,
            string Title,
            string? Notes,
            IReadOnlyList<PublishedTripPlacePayload> Places);

        private sealed record PublishedTripPlacePayload(
            Guid Id,
            string Name,
            string Category,
            string? Address,
            string? Time,
            string? Duration,
            string? Notes,
            decimal? Cost,
            string? PhotoUrl,
            bool IsClosed);
    }
}