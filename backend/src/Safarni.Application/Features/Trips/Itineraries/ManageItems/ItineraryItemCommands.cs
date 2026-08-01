using MediatR;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Trips.GenerateItinerary;

namespace Safarni.Application.Features.Trips.Itineraries.ManageItems;

public sealed record AddItineraryItemCommand(
    Guid TripId,
    Guid DayId,
    string Title,
    string? Description,
    string? StartTime,
    string? EndTime,
    string? Location,
    string Category,
    decimal? EstimatedCost,
    int? OrderIndex)
    : IRequest<Result<ItineraryResponse>>;

public sealed record AddRecommendedItineraryItemCommand(
    Guid TripId,
    Guid DayId,
    string Category,
    string PlaceId,
    int? OrderIndex)
    : IRequest<Result<ItineraryResponse>>;

public sealed record UpdateItineraryItemCommand(
    Guid TripId,
    Guid ItemId,
    string Title,
    string? Description,
    string? StartTime,
    string? EndTime,
    string? Location,
    string Category,
    decimal? EstimatedCost)
    : IRequest<Result<ItineraryResponse>>;

public sealed record MoveItineraryItemCommand(
    Guid TripId,
    Guid ItemId,
    Guid TargetDayId,
    int TargetOrderIndex)
    : IRequest<Result<ItineraryResponse>>;

public sealed record DeleteItineraryItemCommand(
    Guid TripId,
    Guid ItemId)
    : IRequest<Result<ItineraryResponse>>;

public sealed record SelectItineraryVenueCommand(
    Guid TripId,
    Guid ItemId,
    string PlaceId)
    : IRequest<Result<ItineraryResponse>>;
