using MediatR;
using Safarni.Application.Common.Results;
using System;
using System.Collections.Generic;

namespace Safarni.Application.Features.PublicTrips.Queries;

public sealed record SearchPublicTripsQuery(
    string? Destination,
    int? MinDuration,
    int? MaxDuration,
    decimal? MaxBudget,
    string? SortOrder
) : IRequest<Result<IReadOnlyCollection<PublicTripSearchResponse>>>;

public sealed record PublicTripSearchResponse(
    Guid VersionId,
    Guid TripId,
    string TripName,
    string Destination,
    DateTime StartDate,
    DateTime EndDate,
    int DurationDays,
    decimal? CostAmount,
    string AuthorName,
    string AuthorPhotoUrl,
    int ViewsCount,
    int LikesCount,
    double AverageRating,
    DateTime PublishedAt,
    bool IsNewTrip = false,
    [property: System.Text.Json.Serialization.JsonIgnore] string ItineraryJson = ""
);
