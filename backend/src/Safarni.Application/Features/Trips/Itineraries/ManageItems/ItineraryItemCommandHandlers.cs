using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Travel;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Trips.GenerateItinerary;
using Safarni.Application.Features.Trips.Itineraries.VenueSuggestions;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.Itineraries.ManageItems;

public sealed class AddItineraryItemCommandHandler
    : IRequestHandler<AddItineraryItemCommand, Result<ItineraryResponse>>
{
    private readonly ItineraryItemCommandContext _context;
    private readonly IItineraryRouteService? _routeService;

    public AddItineraryItemCommandHandler(
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
        AddItineraryItemCommand request,
        CancellationToken cancellationToken)
    {
        var owned = await _context.GetOwnedAsync(
            request.TripId,
            cancellationToken);

        if (owned.Error is not null)
        {
            return Result<ItineraryResponse>.Failure(owned.Error);
        }

        var itinerary = owned.Itinerary!;
        var day = itinerary.Days.FirstOrDefault(candidate =>
            candidate.Id == request.DayId);

        if (day is null)
        {
            return Result<ItineraryResponse>.Failure(
                ItineraryErrors.DayNotFound);
        }

        var item = new ItineraryItem
        {
            Id = Guid.NewGuid(),
            ItineraryDayId = day.Id,
            Title = request.Title.Trim(),
            Description = NullIfWhiteSpace(request.Description),
            StartTime = ItineraryItemInput.ParseTime(request.StartTime),
            EndTime = ItineraryItemInput.ParseTime(request.EndTime),
            Location = NullIfWhiteSpace(request.Location),
            Category = ItineraryItemInput.ParseCategory(request.Category),
            EstimatedCost = request.EstimatedCost,
            CostSource = request.EstimatedCost.HasValue
                ? "manual_estimate"
                : null
        };

        day.Items.Add(item);
        ItineraryOrder.PlaceItem(
            day,
            item,
            request.OrderIndex ?? day.Items.Count - 1);

        await RebuildRoutesAsync(
            itinerary,
            [day],
            _routeService,
            cancellationToken);

        itinerary.UpdatedAt = DateTime.UtcNow;
        await _context.SaveAsync(cancellationToken);

        return Result<ItineraryResponse>.Success(
            ItineraryResponseMapper.Map(itinerary));
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal static Task RebuildRoutesAsync(
        Itinerary itinerary,
        IEnumerable<ItineraryDay> days,
        IItineraryRouteService? routeService,
        CancellationToken cancellationToken)
    {
        var preferences = GenerateItineraryPreferencesSerializer
            .Deserialize(itinerary.Preferences);

        return ItineraryRouteEnricher.EnrichDaysAsync(
            days,
            preferences,
            routeService,
            cancellationToken);
    }
}

public sealed class AddRecommendedItineraryItemCommandHandler
    : IRequestHandler<
        AddRecommendedItineraryItemCommand,
        Result<ItineraryResponse>>
{
    private readonly ItineraryItemCommandContext _context;
    private readonly ItineraryVenueSuggestionLoader _loader;
    private readonly IItineraryRouteService? _routeService;

    public AddRecommendedItineraryItemCommandHandler(
        ICurrentUserService currentUserService,
        ITripRepository tripRepository,
        IItineraryRepository itineraryRepository,
        ItineraryVenueSuggestionLoader loader,
        IItineraryRouteService? routeService = null)
    {
        _context = new ItineraryItemCommandContext(
            currentUserService,
            tripRepository,
            itineraryRepository);
        _loader = loader;
        _routeService = routeService;
    }

    public async Task<Result<ItineraryResponse>> Handle(
        AddRecommendedItineraryItemCommand request,
        CancellationToken cancellationToken)
    {
        var owned = await _context.GetOwnedAsync(
            request.TripId,
            cancellationToken);

        if (owned.Error is not null)
        {
            return Result<ItineraryResponse>.Failure(owned.Error);
        }

        var trip = owned.Trip!;
        var itinerary = owned.Itinerary!;
        var day = itinerary.Days.FirstOrDefault(candidate =>
            candidate.Id == request.DayId);

        if (day is null)
        {
            return Result<ItineraryResponse>.Failure(
                ItineraryErrors.DayNotFound);
        }

        var category = ItineraryItemInput.ParseCategory(request.Category);
        LoadedVenueSuggestion? selected;

        try
        {
            selected = await _loader.FindByPlaceIdAsync(
                trip,
                itinerary,
                day,
                category,
                request.PlaceId,
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Result<ItineraryResponse>.Failure(
                TripErrors.CorruptRoute);
        }

        if (selected is null)
        {
            return Result<ItineraryResponse>.Failure(
                ItineraryErrors.VenueNotFound);
        }

        var item = new ItineraryItem
        {
            Id = Guid.NewGuid(),
            ItineraryDayId = day.Id,
            Title = category == ItineraryItemCategory.Meal
                ? "Meal"
                : selected.Place.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(
                selected.Place.Description)
                    ? null
                    : selected.Place.Description.Trim(),
            Category = category
        };

        SelectItineraryVenueCommandHandler.ApplyVerifiedVenue(
            item,
            selected,
            isNewItem: true);

        day.Items.Add(item);
        ItineraryOrder.PlaceItem(
            day,
            item,
            request.OrderIndex ?? day.Items.Count - 1);

        await AddItineraryItemCommandHandler.RebuildRoutesAsync(
            itinerary,
            [day],
            _routeService,
            cancellationToken);

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
    private readonly IItineraryRouteService? _routeService;

    public UpdateItineraryItemCommandHandler(
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
        UpdateItineraryItemCommand request,
        CancellationToken cancellationToken)
    {
        var owned = await _context.GetOwnedAsync(
            request.TripId,
            cancellationToken);

        if (owned.Error is not null)
        {
            return Result<ItineraryResponse>.Failure(owned.Error);
        }

        var itinerary = owned.Itinerary!;
        var (day, item) = FindItem(itinerary, request.ItemId);

        if (day is null || item is null)
        {
            return Result<ItineraryResponse>.Failure(
                ItineraryErrors.ItemNotFound);
        }

        var newCategory = ItineraryItemInput.ParseCategory(
            request.Category);
        var newTitle = request.Title.Trim();
        var newLocation = NullIfWhiteSpace(request.Location);
        var identityChanged =
            !string.Equals(item.Title, newTitle, StringComparison.Ordinal) ||
            !string.Equals(item.Location, newLocation, StringComparison.Ordinal) ||
            item.Category != newCategory;
        var costChanged = item.EstimatedCost != request.EstimatedCost;

        item.Title = newTitle;
        item.Description = NullIfWhiteSpace(request.Description);
        item.StartTime = ItineraryItemInput.ParseTime(request.StartTime);
        item.EndTime = ItineraryItemInput.ParseTime(request.EndTime);
        item.Location = newLocation;
        item.Category = newCategory;
        item.EstimatedCost = request.EstimatedCost;

        if (identityChanged)
        {
            ClearVerifiedMetadata(item);
        }

        if (identityChanged || costChanged)
        {
            item.CostSource = request.EstimatedCost.HasValue
                ? "manual_estimate"
                : null;
        }

        await AddItineraryItemCommandHandler.RebuildRoutesAsync(
            itinerary,
            [day],
            _routeService,
            cancellationToken);

        itinerary.UpdatedAt = DateTime.UtcNow;
        await _context.SaveAsync(cancellationToken);

        return Result<ItineraryResponse>.Success(
            ItineraryResponseMapper.Map(itinerary));
    }

    private static (ItineraryDay? Day, ItineraryItem? Item) FindItem(
        Itinerary itinerary,
        Guid itemId)
    {
        var day = itinerary.Days.FirstOrDefault(candidate =>
            candidate.Items.Any(item => item.Id == itemId));
        return (day, day?.Items.FirstOrDefault(item => item.Id == itemId));
    }

    private static void ClearVerifiedMetadata(ItineraryItem item)
    {
        item.GooglePlaceId = null;
        item.Address = null;
        item.GoogleMapsUrl = null;
        item.Rating = null;
        item.PhotoUrl = null;
        item.Latitude = null;
        item.Longitude = null;
        item.PriceLevel = null;
        item.RatePerNight = null;
        item.StayNights = null;
        item.PriceSource = null;
        item.ExternalUrl = null;
        item.FlightDepartureAirportCode = null;
        item.FlightDepartureAirportName = null;
        item.FlightArrivalAirportCode = null;
        item.FlightArrivalAirportName = null;
        item.FlightAirlines = null;
        item.FlightNumbers = null;
        item.FlightDurationMinutes = null;
        item.FlightStopCount = null;
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class MoveItineraryItemCommandHandler
    : IRequestHandler<MoveItineraryItemCommand, Result<ItineraryResponse>>
{
    private readonly ItineraryItemCommandContext _context;
    private readonly IItineraryRouteService? _routeService;

    public MoveItineraryItemCommandHandler(
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
        MoveItineraryItemCommand request,
        CancellationToken cancellationToken)
    {
        var owned = await _context.GetOwnedAsync(
            request.TripId,
            cancellationToken);

        if (owned.Error is not null)
        {
            return Result<ItineraryResponse>.Failure(owned.Error);
        }

        var itinerary = owned.Itinerary!;
        var sourceDay = itinerary.Days.FirstOrDefault(day =>
            day.Items.Any(item => item.Id == request.ItemId));
        var targetDay = itinerary.Days.FirstOrDefault(day =>
            day.Id == request.TargetDayId);
        var item = sourceDay?.Items.FirstOrDefault(candidate =>
            candidate.Id == request.ItemId);

        if (sourceDay is null || item is null)
        {
            return Result<ItineraryResponse>.Failure(
                ItineraryErrors.ItemNotFound);
        }

        if (targetDay is null)
        {
            return Result<ItineraryResponse>.Failure(
                ItineraryErrors.DayNotFound);
        }

        if (sourceDay.Id != targetDay.Id)
        {
            sourceDay.Items.Remove(item);
            ItineraryOrder.Normalize(sourceDay);
            targetDay.Items.Add(item);
            item.ItineraryDayId = targetDay.Id;
            item.ItineraryDay = targetDay;
        }

        ItineraryOrder.PlaceItem(
            targetDay,
            item,
            request.TargetOrderIndex);

        await AddItineraryItemCommandHandler.RebuildRoutesAsync(
            itinerary,
            sourceDay.Id == targetDay.Id
                ? [targetDay]
                : [sourceDay, targetDay],
            _routeService,
            cancellationToken);

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
    private readonly IItineraryRouteService? _routeService;

    public DeleteItineraryItemCommandHandler(
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
        DeleteItineraryItemCommand request,
        CancellationToken cancellationToken)
    {
        var owned = await _context.GetOwnedAsync(
            request.TripId,
            cancellationToken);

        if (owned.Error is not null)
        {
            return Result<ItineraryResponse>.Failure(owned.Error);
        }

        var itinerary = owned.Itinerary!;
        var day = itinerary.Days.FirstOrDefault(candidate =>
            candidate.Items.Any(item => item.Id == request.ItemId));
        var item = day?.Items.FirstOrDefault(candidate =>
            candidate.Id == request.ItemId);

        if (day is null || item is null)
        {
            return Result<ItineraryResponse>.Failure(
                ItineraryErrors.ItemNotFound);
        }

        day.Items.Remove(item);
        _context.RemoveItem(item);
        ItineraryOrder.Normalize(day);

        await AddItineraryItemCommandHandler.RebuildRoutesAsync(
            itinerary,
            [day],
            _routeService,
            cancellationToken);

        itinerary.UpdatedAt = DateTime.UtcNow;
        await _context.SaveAsync(cancellationToken);

        return Result<ItineraryResponse>.Success(
            ItineraryResponseMapper.Map(itinerary));
    }
}

public sealed class SelectItineraryVenueCommandHandler
    : IRequestHandler<SelectItineraryVenueCommand, Result<ItineraryResponse>>
{
    private readonly ItineraryItemCommandContext _context;
    private readonly ItineraryVenueSuggestionLoader _loader;
    private readonly IItineraryRouteService? _routeService;

    public SelectItineraryVenueCommandHandler(
        ICurrentUserService currentUserService,
        ITripRepository tripRepository,
        IItineraryRepository itineraryRepository,
        ItineraryVenueSuggestionLoader loader,
        IItineraryRouteService? routeService = null)
    {
        _context = new ItineraryItemCommandContext(
            currentUserService,
            tripRepository,
            itineraryRepository);
        _loader = loader;
        _routeService = routeService;
    }

    public async Task<Result<ItineraryResponse>> Handle(
        SelectItineraryVenueCommand request,
        CancellationToken cancellationToken)
    {
        var owned = await _context.GetOwnedAsync(
            request.TripId,
            cancellationToken);

        if (owned.Error is not null)
        {
            return Result<ItineraryResponse>.Failure(owned.Error);
        }

        var trip = owned.Trip!;
        var itinerary = owned.Itinerary!;
        var day = itinerary.Days.FirstOrDefault(candidate =>
            candidate.Items.Any(item => item.Id == request.ItemId));
        var item = day?.Items.FirstOrDefault(candidate =>
            candidate.Id == request.ItemId);

        if (day is null || item is null)
        {
            return Result<ItineraryResponse>.Failure(
                ItineraryErrors.ItemNotFound);
        }

        if (item.FlightDepartureAirportCode is not null)
        {
            return Result<ItineraryResponse>.Failure(
                ItineraryErrors.UnsupportedVenueSelection);
        }

        LoadedVenueSuggestion? selected;
        try
        {
            selected = await _loader.FindByPlaceIdAsync(
                trip,
                itinerary,
                day,
                item,
                request.PlaceId,
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Result<ItineraryResponse>.Failure(
                TripErrors.CorruptRoute);
        }

        if (selected is null)
        {
            return Result<ItineraryResponse>.Failure(
                ItineraryErrors.VenueNotFound);
        }

        ApplyVerifiedVenue(item, selected);

        await AddItineraryItemCommandHandler.RebuildRoutesAsync(
            itinerary,
            [day],
            _routeService,
            cancellationToken);

        itinerary.UpdatedAt = DateTime.UtcNow;
        await _context.SaveAsync(cancellationToken);

        return Result<ItineraryResponse>.Success(
            ItineraryResponseMapper.Map(itinerary));
    }

    internal static void ApplyVerifiedVenue(
        ItineraryItem item,
        LoadedVenueSuggestion selected,
        bool isNewItem = false)
    {
        var place = selected.Place;

        item.GooglePlaceId = place.PlaceId;
        item.Address = NullIfWhiteSpace(place.Address);
        item.GoogleMapsUrl = NullIfWhiteSpace(place.GoogleMapsLink);
        item.Rating = place.Rating;
        item.PhotoUrl = NullIfWhiteSpace(place.PhotoUrl);
        item.Latitude = place.Latitude;
        item.Longitude = place.Longitude;
        item.PriceLevel = NullIfWhiteSpace(place.PriceLevel);
        item.RatePerNight = place.RatePerNight;
        item.StayNights = selected.StayNights;
        item.PriceSource = NullIfWhiteSpace(place.PriceSource);
        item.ExternalUrl = NullIfWhiteSpace(place.ExternalUrl);
        item.Location = place.Name.Trim();

        if (item.Category is not
            (ItineraryItemCategory.Accommodation or
             ItineraryItemCategory.Meal))
        {
            item.Title = place.Name.Trim();
            item.Description = NullIfWhiteSpace(place.Description);
            item.EstimatedCost = null;
            item.CostSource = null;
            return;
        }

        if (item.Category == ItineraryItemCategory.Accommodation)
        {
            item.Title = place.Name.Trim();
            item.Description = NullIfWhiteSpace(place.Description);
            item.EstimatedCost = place.TotalRate;
            item.CostSource = place.TotalRate.HasValue
                ? "live_hotel_rate"
                : null;
        }
        else
        {
            item.Title =
                isNewItem
                    ? $"Meal — {place.Name.Trim()}"
                    : $"{GetMealSlotTitle(item.Title)} — {place.Name.Trim()}";
            item.Description = NullIfWhiteSpace(place.Description);
        }
    }

    private static string GetMealSlotTitle(string currentTitle)
    {
        var separators = new[] { '—', '-', ':' };
        var separatorIndex = currentTitle.IndexOfAny(separators);
        return separatorIndex > 0
            ? currentTitle[..separatorIndex].Trim()
            : currentTitle.Trim();
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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

    public async Task<(
        Trip? Trip,
        Itinerary? Itinerary,
        Error? Error)> GetOwnedAsync(
        Guid tripId,
        CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return (
                null,
                null,
                AuthenticationErrors.AuthenticationRequired);
        }

        var trip = await _tripRepository.GetByIdAsync(
            tripId,
            cancellationToken);

        if (trip is null)
        {
            return (null, null, ItineraryErrors.TripNotFound);
        }

        if (trip.UserId != userId)
        {
            return (null, null, ItineraryErrors.Forbidden);
        }

        var itinerary = await _itineraryRepository.GetActiveByTripIdAsync(
            tripId,
            cancellationToken);

        return itinerary is null
            ? (trip, null, ItineraryErrors.NotFound)
            : (trip, itinerary, null);
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

        for (var currentIndex = 0;
             currentIndex < ordered.Count;
             currentIndex++)
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
