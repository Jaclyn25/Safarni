using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Trips;
using Safarni.Application.Features.Trips.CreateTrip;
using Safarni.Application.Features.Trips.GenerateItinerary;
using Safarni.Application.Features.Trips.Itineraries.GetActiveItinerary;
using Safarni.Application.Features.Trips.Itineraries.ManageItems;
using Safarni.Application.Features.Trips.Itineraries.VenueSuggestions;
using Safarni.Application.Features.Trips.DeleteTrip;
using Safarni.Application.Features.Trips.UpdateTrip;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize]
[Route("api/trips")]
public sealed class TripsController : ControllerBase
{
    private readonly ISender _sender;

    public TripsController(ISender sender)
    {
        _sender = sender;
    }

    // =========================================================
    // CREATE TRIP
    // POST: /api/trips
    // =========================================================

    [HttpPost]
    [ProducesResponseType(
        typeof(ApiResponse<TripResponse>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ApiResponse<TripResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<TripResponse>),
        StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateTrip(
        [FromBody] CreateTripCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            return Failure<TripResponse>(
                result.Error,
                "Failed to create trip.");
        }

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<TripResponse>.SuccessResponse(
                result.Value!,
                "Trip created successfully.",
                StatusCodes.Status201Created));
    }

    // =========================================================
    // UPDATE TRIP
    // PUT: /api/trips/{id}
    // =========================================================

    [HttpPut("{id:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<TripResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<TripResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<TripResponse>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<TripResponse>),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiResponse<TripResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTrip(
        Guid id,
        [FromBody] UpdateTripRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTripCommand(
            id,
            request.Name,
            request.Destination,
            request.StartDate,
            request.EndDate,
            request.OriginCountry,
            request.Cities);

        var result = await _sender.Send(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            return Failure<TripResponse>(
                result.Error,
                "Failed to update trip.");
        }

        return Ok(
            ApiResponse<TripResponse>.SuccessResponse(
                result.Value!,
                "Trip updated successfully.",
                StatusCodes.Status200OK));
    }

    // =========================================================
    // GET ACTIVE ITINERARY
    // GET: /api/trips/{id}/itinerary
    // =========================================================

    [HttpGet("{id:guid}/itinerary")]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveItinerary(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetActiveItineraryQuery(id);

        var result = await _sender.Send(
            query,
            cancellationToken);

        if (result.IsFailure)
        {
            return Failure<ItineraryResponse>(
                result.Error,
                "Failed to load itinerary.");
        }

        return Ok(
            ApiResponse<ItineraryResponse>.SuccessResponse(
                result.Value!,
                "Itinerary loaded successfully.",
                StatusCodes.Status200OK));
    }

    // =========================================================
    // GENERATE ITINERARY WITHOUT STREAMING
    // POST: /api/trips/{id}/itinerary/generate
    // =========================================================

    [HttpPost("{id:guid}/itinerary/generate")]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateItinerary(
        Guid id,
        [FromBody] GenerateItineraryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new GenerateItineraryCommand(
            id,
            request.Preferences,
            request.BudgetAmount,
            request.CurrencyCode,
            request.TravelerCount);

        var result = await _sender.Send(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            return Failure<ItineraryResponse>(
                result.Error,
                "Failed to generate itinerary.");
        }

        return Ok(
            ApiResponse<ItineraryResponse>.SuccessResponse(
                result.Value!,
                "Itinerary generated and saved successfully.",
                StatusCodes.Status200OK));
    }

    // =========================================================
    // ADD ITINERARY ITEM
    // POST: /api/trips/{id}/itinerary/items
    // =========================================================

    [HttpPost("{id:guid}/itinerary/items")]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItineraryItem(
        Guid id,
        [FromBody] AddItineraryItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddItineraryItemCommand(
            id,
            request.DayId,
            request.Title,
            request.Description,
            request.StartTime,
            request.EndTime,
            request.Location,
            request.Category,
            request.EstimatedCost,
            request.OrderIndex);

        var result = await _sender.Send(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            return Failure<ItineraryResponse>(
                result.Error,
                "Failed to add itinerary item.");
        }

        return Ok(
            ApiResponse<ItineraryResponse>.SuccessResponse(
                result.Value!,
                "Itinerary item added successfully.",
                StatusCodes.Status200OK));
    }

    // =========================================================
    // UPDATE OR MOVE ITINERARY ITEM
    // PUT: /api/trips/{id}/itinerary/items/{itemId}
    // =========================================================

    [HttpPut("{id:guid}/itinerary/items/{itemId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItineraryItem(
        Guid id,
        Guid itemId,
        [FromBody] UpdateItineraryItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateItineraryItemCommand(
            id,
            itemId,
            request.TargetDayId,
            request.Title,
            request.Description,
            request.StartTime,
            request.EndTime,
            request.Location,
            request.Category,
            request.EstimatedCost,
            request.OrderIndex);

        var result = await _sender.Send(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            return Failure<ItineraryResponse>(
                result.Error,
                "Failed to update itinerary item.");
        }

        return Ok(
            ApiResponse<ItineraryResponse>.SuccessResponse(
                result.Value!,
                "Itinerary item updated successfully.",
                StatusCodes.Status200OK));
    }

    // =========================================================
    // DELETE ITINERARY ITEM
    // DELETE: /api/trips/{id}/itinerary/items/{itemId}
    // =========================================================

    [HttpDelete("{id:guid}/itinerary/items/{itemId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItineraryItem(
        Guid id,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteItineraryItemCommand(
            id,
            itemId);

        var result = await _sender.Send(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            return Failure<ItineraryResponse>(
                result.Error,
                "Failed to delete itinerary item.");
        }

        return Ok(
            ApiResponse<ItineraryResponse>.SuccessResponse(
                result.Value!,
                "Itinerary item deleted successfully.",
                StatusCodes.Status200OK));
    }

    [HttpGet("{id:guid}/itinerary/items/{itemId:guid}/venues")]
    [ProducesResponseType(
        typeof(ApiResponse<
            IReadOnlyList<VenueSuggestionResponse>>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVenueSuggestions(
        Guid id,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetVenueSuggestionsQuery(id, itemId),
            cancellationToken);

        if (result.IsFailure)
        {
            return Failure<
                IReadOnlyList<VenueSuggestionResponse>>(
                result.Error,
                "Failed to load venue suggestions.");
        }

        return Ok(
            ApiResponse<
                IReadOnlyList<VenueSuggestionResponse>>
                .SuccessResponse(
                    result.Value!,
                    "Venue suggestions loaded successfully.",
                    StatusCodes.Status200OK));
    }

    [HttpPut("{id:guid}/itinerary/items/{itemId:guid}/venue")]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> SelectVenue(
        Guid id,
        Guid itemId,
        [FromBody] SelectVenueRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SelectItineraryVenueCommand(
            id,
            itemId,
            request.PlaceId,
            request.Name,
            request.Description,
            request.Address,
            request.GoogleMapsUrl,
            request.Rating,
            request.PhotoUrl,
            request.PriceLevel,
            request.RatePerNight,
            request.TotalRate,
            request.PriceSource,
            request.ExternalUrl,
            request.Latitude,
            request.Longitude);

        var result = await _sender.Send(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            return Failure<ItineraryResponse>(
                result.Error,
                "Failed to select the venue.");
        }

        return Ok(
            ApiResponse<ItineraryResponse>
                .SuccessResponse(
                    result.Value!,
                    "Venue selected successfully.",
                    StatusCodes.Status200OK));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTrip(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteTripCommand(id);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Failure<object>(result.Error, "Failed to delete trip.");
        }

        return NoContent();
    }

    // =========================================================
    // RESPONSE HELPERS
    // =========================================================

    private ObjectResult Failure<T>(Error error, string message)
    {
        var statusCode = MapStatusCode(error.Type);

        var response = ApiResponse<T>.FailureResponse(
            [
                new ApiError(
                    error.Code,
                    error.Message)
            ],
            message,
            statusCode);

        return StatusCode(
            statusCode,
            response);
    }

    private static int MapStatusCode(
        ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.Validation =>
                StatusCodes.Status400BadRequest,

            ErrorType.Conflict =>
                StatusCodes.Status409Conflict,

            ErrorType.NotFound =>
                StatusCodes.Status404NotFound,

            ErrorType.Unauthorized =>
                StatusCodes.Status401Unauthorized,

            ErrorType.Forbidden =>
                StatusCodes.Status403Forbidden,

            _ =>
                StatusCodes.Status500InternalServerError
        };
    }
}

// =============================================================
// REQUEST MODELS
// =============================================================

public sealed record UpdateTripRequest(
    string Name,
    string Destination,
    DateTime StartDate,
    DateTime EndDate,
    string OriginCountry,
    IReadOnlyList<TripCitySelection> Cities);

public sealed record GenerateItineraryRequest(
    GenerateItineraryPreferences Preferences,
    decimal BudgetAmount,
    string CurrencyCode,
    int TravelerCount);

public sealed record AddItineraryItemRequest(
    Guid DayId,
    string Title,
    string? Description,
    string? StartTime,
    string? EndTime,
    string? Location,
    string Category,
    decimal? EstimatedCost,
    int? OrderIndex);

public sealed record UpdateItineraryItemRequest(
    Guid TargetDayId,
    string Title,
    string? Description,
    string? StartTime,
    string? EndTime,
    string? Location,
    string Category,
    decimal? EstimatedCost,
    int OrderIndex);

public sealed record SelectVenueRequest(
    string PlaceId,
    string Name,
    string? Description,
    string? Address,
    string? GoogleMapsUrl,
    double? Rating,
    string? PhotoUrl,
    string? PriceLevel,
    decimal? RatePerNight,
    decimal? TotalRate,
    string? PriceSource,
    string? ExternalUrl,
    double? Latitude,
    double? Longitude);
