using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Travel;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Trips.GenerateItinerary;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.Itineraries.ManageItems;

public sealed class AddItineraryItemCommandHandler
    : IRequestHandler<AddItineraryItemCommand, Result<ItineraryResponse>>
{
    private readonly ItineraryItemCommandContext _context;

    public AddItineraryItemCommandHandler(
        ICurrentUserService currentUserService,
        ITripRepository tripRepository,
        IItineraryRepository itineraryRepository)
    {
        _context = new ItineraryItemCommandContext(
            currentUserService,
            tripRepository,
            itineraryRepository);
    }

    public async Task<Result<ItineraryResponse>> Handle(
        AddItineraryItemCommand request,
        CancellationToken cancellationToken)
    {
        var access = await _context.GetOwnedItineraryAsync(
            request.TripId,
            cancellationToken);

        if (access.Error is not null)
        {
            return Result<ItineraryResponse>.Failure(access.Error);
        }

        var itinerary = access.Itinerary!;
        var day = itinerary.Days.FirstOrDefault(d => d.Id == request.DayId);
        if (day is null)
        {
            return Result<ItineraryResponse>.Failure(ItineraryErrors.DayNotFound);
        }

        var item = new ItineraryItem
        {
            Id = Guid.NewGuid(),
            ItineraryDayId = day.Id,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            StartTime = ItineraryItemInput.ParseTime(request.StartTime),
            EndTime = ItineraryItemInput.ParseTime(request.EndTime),
            Location = request.Location?.Trim(),
            Category = ItineraryItemInput.ParseCategory(request.Category),
            EstimatedCost = request.EstimatedCost
        };

        day.Items.Add(item);
        ItineraryOrder.PlaceItem(
            day,
            item,
            request.OrderIndex ?? day.Items.Count - 1);

        itinerary.UpdatedAt = DateTime.UtcNow;
        await _context.SaveAsync(cancellationToken);

        return Result<ItineraryResponse>.Success(
            ItineraryResponseMapper.Map(itinerary));
    }
}

public sealed class UpdateItineraryItemCommandHandler
    : IRequestHandler<UpdateItineraryItemCommand, Result<ItineraryResponse>>
{
    private readonly ItineraryItemCommandContext _context;

    public UpdateItineraryItemCommandHandler(
        ICurrentUserService currentUserService,
        ITripRepository tripRepository,
        IItineraryRepository itineraryRepository)
    {
        _context = new ItineraryItemCommandContext(
            currentUserService,
            tripRepository,
            itineraryRepository);
    }

    public async Task<Result<ItineraryResponse>> Handle(
        UpdateItineraryItemCommand request,
        CancellationToken cancellationToken)
    {
        var access = await _context.GetOwnedItineraryAsync(
            request.TripId,
            cancellationToken);

        if (access.Error is not null)
        {
            return Result<ItineraryResponse>.Failure(access.Error);
        }

        var itinerary = access.Itinerary!;
        var sourceDay = itinerary.Days
            .FirstOrDefault(d => d.Items.Any(i => i.Id == request.ItemId));
        var targetDay = itinerary.Days
            .FirstOrDefault(d => d.Id == request.TargetDayId);

        if (sourceDay is null)
        {
            return Result<ItineraryResponse>.Failure(ItineraryErrors.ItemNotFound);
        }

        if (targetDay is null)
        {
            return Result<ItineraryResponse>.Failure(ItineraryErrors.DayNotFound);
        }

        var item = sourceDay.Items.First(i => i.Id == request.ItemId);
        item.Title = request.Title.Trim();
        item.Description = request.Description?.Trim();
        item.StartTime = ItineraryItemInput.ParseTime(request.StartTime);
        item.EndTime = ItineraryItemInput.ParseTime(request.EndTime);
        item.Location = request.Location?.Trim();
        item.Category = ItineraryItemInput.ParseCategory(request.Category);
        item.EstimatedCost = request.EstimatedCost;

        if (sourceDay.Id != targetDay.Id)
        {
            sourceDay.Items.Remove(item);
            ItineraryOrder.Normalize(sourceDay);

            item.ItineraryDayId = targetDay.Id;
            item.ItineraryDay = targetDay;
            targetDay.Items.Add(item);
        }

        ItineraryOrder.PlaceItem(targetDay, item, request.OrderIndex);
        itinerary.UpdatedAt = DateTime.UtcNow;
        await _context.SaveAsync(cancellationToken);

        return Result<ItineraryResponse>.Success(
            ItineraryResponseMapper.Map(itinerary));
    }
}

public sealed class DeleteItineraryItemCommandHandler
    : IRequestHandler<DeleteItineraryItemCommand, Result<ItineraryResponse>>
{
    private readonly ItineraryItemCommandContext _context;

    public DeleteItineraryItemCommandHandler(
        ICurrentUserService currentUserService,
        ITripRepository tripRepository,
        IItineraryRepository itineraryRepository)
    {
        _context = new ItineraryItemCommandContext(
            currentUserService,
            tripRepository,
            itineraryRepository);
    }

    public async Task<Result<ItineraryResponse>> Handle(
        DeleteItineraryItemCommand request,
        CancellationToken cancellationToken)
    {
        var access = await _context.GetOwnedItineraryAsync(
            request.TripId,
            cancellationToken);

        if (access.Error is not null)
        {
            return Result<ItineraryResponse>.Failure(access.Error);
        }

        var itinerary = access.Itinerary!;
        var day = itinerary.Days
            .FirstOrDefault(d => d.Items.Any(i => i.Id == request.ItemId));

        if (day is null)
        {
            return Result<ItineraryResponse>.Failure(ItineraryErrors.ItemNotFound);
        }

        var item = day.Items.First(i => i.Id == request.ItemId);
        day.Items.Remove(item);
        _context.RemoveItem(item);
        ItineraryOrder.Normalize(day);

        itinerary.UpdatedAt = DateTime.UtcNow;
        await _context.SaveAsync(cancellationToken);

        return Result<ItineraryResponse>.Success(
            ItineraryResponseMapper.Map(itinerary));
    }
}

public sealed class SelectItineraryVenueCommandHandler
    : IRequestHandler<
        SelectItineraryVenueCommand,
        Result<ItineraryResponse>>
{
    private readonly ItineraryItemCommandContext _context;
    private readonly IItineraryRouteService? _routeService;

    public SelectItineraryVenueCommandHandler(
        ICurrentUserService currentUserService,
        ITripRepository tripRepository,
        IItineraryRepository itineraryRepository,
        IItineraryRouteService? routeService = null)
    {
        _context = new ItineraryItemCommandContext(
            currentUserService,
            tripRepository,
            itineraryRepository);
        _routeService = routeService;
    }

    public async Task<Result<ItineraryResponse>> Handle(
        SelectItineraryVenueCommand request,
        CancellationToken cancellationToken)
    {
        var access =
            await _context.GetOwnedItineraryAsync(
                request.TripId,
                cancellationToken);

        if (access.Error is not null)
        {
            return Result<ItineraryResponse>.Failure(
                access.Error);
        }

        var itinerary = access.Itinerary!;
        var item = itinerary.Days
            .SelectMany(day => day.Items)
            .FirstOrDefault(candidate =>
                candidate.Id == request.ItemId);

        if (item is null)
        {
            return Result<ItineraryResponse>.Failure(
                ItineraryErrors.ItemNotFound);
        }

        if (item.Category is not
            (ItineraryItemCategory.Meal or
             ItineraryItemCategory.Accommodation))
        {
            return Result<ItineraryResponse>.Failure(
                new Error(
                    "Itinerary.UnsupportedVenueSelection",
                    "Only meal and accommodation venues can be selected.",
                    ErrorType.Validation));
        }

        item.GooglePlaceId = request.PlaceId.Trim();
        item.Location = request.Name.Trim();
        item.Address = request.Address?.Trim();
        item.GoogleMapsUrl =
            request.GoogleMapsUrl?.Trim();
        item.Rating = request.Rating;
        item.PhotoUrl = request.PhotoUrl?.Trim();
        item.PriceLevel = request.PriceLevel?.Trim();
        item.RatePerNight = request.RatePerNight;
        item.PriceSource = request.PriceSource?.Trim();
        item.ExternalUrl = request.ExternalUrl?.Trim();
        item.Latitude = request.Latitude;
        item.Longitude = request.Longitude;

        if (item.Category ==
            ItineraryItemCategory.Accommodation)
        {
            item.Title = request.Name.Trim();
            item.Description =
                request.Description?.Trim();
            item.EstimatedCost = request.TotalRate;
            item.CostSource =
                request.TotalRate is null
                    ? "planning_estimate"
                    : "live_hotel_rate";
        }
        else
        {
            item.Title =
                $"{GetMealSlotTitle(item.Title)} — {request.Name.Trim()}";
        }

        await RefreshAdjacentRoutesAsync(
            itinerary,
            item,
            cancellationToken);

        itinerary.UpdatedAt = DateTime.UtcNow;
        await _context.SaveAsync(cancellationToken);

        return Result<ItineraryResponse>.Success(
            ItineraryResponseMapper.Map(itinerary));
    }

    private static string GetMealSlotTitle(
        string currentTitle)
    {
        var separatorIndex =
            currentTitle.IndexOf('—');

        if (separatorIndex > 0)
        {
            return currentTitle[..separatorIndex].Trim();
        }

        return currentTitle.Trim();
    }

    private async Task RefreshAdjacentRoutesAsync(
        Itinerary itinerary,
        ItineraryItem selectedItem,
        CancellationToken cancellationToken)
    {
        var items = itinerary.Days
            .OrderBy(day => day.DayNumber)
            .SelectMany(day => day.Items
                .OrderBy(item => item.OrderIndex))
            .ToList();
        var selectedIndex =
            items.FindIndex(item =>
                item.Id == selectedItem.Id);

        if (selectedIndex < 0)
        {
            return;
        }

        var previousPlace = items
            .Take(selectedIndex)
            .LastOrDefault(HasCoordinates);
        var nextPlace = items
            .Skip(selectedIndex + 1)
            .FirstOrDefault(HasCoordinates);

        ClearRoute(selectedItem);

        if (nextPlace is not null)
        {
            ClearRoute(nextPlace);
        }

        if (_routeService?.IsConfigured != true)
        {
            return;
        }

        var preferences =
            GenerateItineraryPreferencesSerializer
                .Deserialize(itinerary.Preferences);

        if (previousPlace is not null &&
            HasCoordinates(selectedItem))
        {
            await SetRouteAsync(
                previousPlace,
                selectedItem,
                preferences,
                cancellationToken);
        }

        if (nextPlace is not null)
        {
            var origin = HasCoordinates(selectedItem)
                ? selectedItem
                : previousPlace;

            if (origin is not null)
            {
                await SetRouteAsync(
                    origin,
                    nextPlace,
                    preferences,
                    cancellationToken);
            }
        }
    }

    private async Task SetRouteAsync(
        ItineraryItem origin,
        ItineraryItem destination,
        GenerateItineraryPreferences preferences,
        CancellationToken cancellationToken)
    {
        var route = await _routeService!
            .ComputeRouteAsync(
                new ItineraryRoutePoint(
                    origin.Latitude!.Value,
                    origin.Longitude!.Value),
                new ItineraryRoutePoint(
                    destination.Latitude!.Value,
                    destination.Longitude!.Value),
                preferences.Transportation,
                preferences.PreferredLanguage,
                cancellationToken);

        if (route is null)
        {
            return;
        }

        destination.RouteDistanceMeters =
            route.DistanceMeters;
        destination.RouteDurationMinutes =
            route.DurationMinutes;
        destination.RouteTravelMode =
            route.TravelMode;
        destination.RouteTransitDetails =
            route.TransitDetails;
    }

    private static bool HasCoordinates(
        ItineraryItem item) =>
        item.Latitude is not null &&
        item.Longitude is not null;

    private static void ClearRoute(
        ItineraryItem item)
    {
        item.RouteDistanceMeters = null;
        item.RouteDurationMinutes = null;
        item.RouteTravelMode = null;
        item.RouteTransitDetails = null;
    }
}

internal sealed class ItineraryItemCommandContext
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITripRepository _tripRepository;
    private readonly IItineraryRepository _itineraryRepository;

    public ItineraryItemCommandContext(
        ICurrentUserService currentUserService,
        ITripRepository tripRepository,
        IItineraryRepository itineraryRepository)
    {
        _currentUserService = currentUserService;
        _tripRepository = tripRepository;
        _itineraryRepository = itineraryRepository;
    }

    public async Task<(Itinerary? Itinerary, Error? Error)> GetOwnedItineraryAsync(
        Guid tripId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId is null)
        {
            return (null, AuthenticationErrors.AuthenticationRequired);
        }

        var trip = await _tripRepository.GetByIdAsync(tripId, cancellationToken);
        if (trip is null)
        {
            return (null, ItineraryErrors.TripNotFound);
        }

        if (trip.UserId != userId.Value)
        {
            return (null, ItineraryErrors.Forbidden);
        }

        var itinerary = await _itineraryRepository.GetActiveByTripIdAsync(
            tripId,
            cancellationToken);

        return itinerary is null
            ? (null, ItineraryErrors.NotFound)
            : (itinerary, null);
    }

    public void RemoveItem(ItineraryItem item)
    {
        _itineraryRepository.RemoveItem(item);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _itineraryRepository.SaveAsync(cancellationToken);
    }
}

internal static class ItineraryOrder
{
    public static void PlaceItem(
        ItineraryDay day,
        ItineraryItem item,
        int requestedIndex)
    {
        var ordered = day.Items
            .Where(existing => existing.Id != item.Id)
            .OrderBy(existing => existing.OrderIndex)
            .ToList();

        var index = Math.Clamp(requestedIndex, 0, ordered.Count);
        ordered.Insert(index, item);

        for (var currentIndex = 0; currentIndex < ordered.Count; currentIndex++)
        {
            ordered[currentIndex].OrderIndex = currentIndex;
        }
    }

    public static void Normalize(ItineraryDay day)
    {
        var ordered = day.Items
            .OrderBy(item => item.OrderIndex)
            .ToList();

        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].OrderIndex = index;
        }
    }
}
