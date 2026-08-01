using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Trips;
using Safarni.Application.Features.Trips.CreateTrip;
using Safarni.Application.Features.Trips.DeleteTrip;
using Safarni.Application.Features.Trips.GenerateItinerary;
using Safarni.Application.Features.Trips.GenerateItinerary.Jobs;
using Safarni.Application.Features.Trips.GetTrip;
using Safarni.Application.Features.Trips.ListTrips;
using Safarni.Application.Features.Trips.Itineraries.GetActiveItinerary;
using Safarni.Application.Features.Trips.Itineraries.ManageItems;
using Safarni.Application.Features.Trips.Itineraries.VenueSuggestions;
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

    [HttpPost]
    [ProducesResponseType(
        typeof(ApiResponse<TripResponse>),
        StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTrip(
        [FromBody] CreateTripCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Failure<TripResponse>(
                result.Error,
                "Failed to create trip.");
        }

        return CreatedAtAction(
            nameof(GetTrip),
            new { id = result.Value!.Id },
            ApiResponse<TripResponse>.SuccessResponse(
                result.Value,
                "Trip created successfully.",
                StatusCodes.Status201Created));
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyCollection<TripResponse>>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTrips(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ListTripsQuery(),
            cancellationToken);

        if (result.IsFailure)
        {
            return Failure<IReadOnlyCollection<TripResponse>>(
                result.Error,
                "Failed to load trips.");
        }

        return Ok(ApiResponse<IReadOnlyCollection<TripResponse>>
            .SuccessResponse(
                result.Value!,
                "Trips loaded successfully.",
                StatusCodes.Status200OK));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<TripResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrip(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetTripQuery(id),
            cancellationToken);

        if (result.IsFailure)
        {
            return Failure<TripResponse>(
                result.Error,
                "Failed to load trip.");
        }

        return Ok(ApiResponse<TripResponse>.SuccessResponse(
            result.Value!,
            "Trip loaded successfully.",
            StatusCodes.Status200OK));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<TripResponse>),
        StatusCodes.Status200OK)]
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
            request.Cities,
            request.OriginCountryCode,
            request.OriginCity,
            request.OriginAirportCode,
            request.OriginAirportName);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Failure<TripResponse>(
                result.Error,
                "Failed to update trip.");
        }

        return Ok(ApiResponse<TripResponse>.SuccessResponse(
            result.Value!,
            "Trip updated successfully.",
            StatusCodes.Status200OK));
    }

    [HttpGet("{id:guid}/itinerary")]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveItinerary(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetActiveItineraryQuery(id),
            cancellationToken);

        if (result.IsFailure)
        {
            return Failure<ItineraryResponse>(
                result.Error,
                "Failed to load itinerary.");
        }

        return Ok(ApiResponse<ItineraryResponse>.SuccessResponse(
            result.Value!,
            "Itinerary loaded successfully.",
            StatusCodes.Status200OK));
    }

    /// <summary>
    /// Queues durable itinerary generation and returns immediately. The client
    /// polls the job endpoint until status is completed, failed, or cancelled.
    /// </summary>
    [HttpPost("{id:guid}/itinerary/generate")]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryGenerationJobResponse>),
        StatusCodes.Status202Accepted)]
    public async Task<IActionResult> GenerateItinerary(
        Guid id,
        [FromBody] GenerateItineraryRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new StartItineraryGenerationCommand(
            id,
            request.Preferences,
            request.BudgetAmount,
            request.CurrencyCode,
            request.TravelerCount,
            idempotencyKey);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Failure<ItineraryGenerationJobResponse>(
                result.Error,
                "Failed to queue itinerary generation.");
        }

        var response = ApiResponse<ItineraryGenerationJobResponse>
            .SuccessResponse(
                result.Value!,
                "Itinerary generation queued.",
                StatusCodes.Status202Accepted);

        return AcceptedAtAction(
            nameof(GetItineraryGenerationJob),
            new { id, jobId = result.Value!.Id },
            response);
    }

    [HttpGet("{id:guid}/itinerary/generation/{jobId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryGenerationJobResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetItineraryGenerationJob(
        Guid id,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetItineraryGenerationJobQuery(id, jobId),
            cancellationToken);

        if (result.IsFailure)
        {
            return Failure<ItineraryGenerationJobResponse>(
                result.Error,
                "Failed to load itinerary generation status.");
        }

        return Ok(ApiResponse<ItineraryGenerationJobResponse>
            .SuccessResponse(
                result.Value!,
                "Itinerary generation status loaded.",
                StatusCodes.Status200OK));
    }

    [HttpDelete("{id:guid}/itinerary/generation/{jobId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryGenerationJobResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelItineraryGeneration(
        Guid id,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CancelItineraryGenerationCommand(id, jobId),
            cancellationToken);

        if (result.IsFailure)
        {
            return Failure<ItineraryGenerationJobResponse>(
                result.Error,
                "Failed to cancel itinerary generation.");
        }

        return Ok(ApiResponse<ItineraryGenerationJobResponse>
            .SuccessResponse(
                result.Value!,
                "Cancellation requested.",
                StatusCodes.Status200OK));
    }

    [HttpPost("{id:guid}/itinerary/items")]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status200OK)]
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

        return await SendItineraryMutation(
            command,
            "Itinerary item added successfully.",
            "Failed to add itinerary item.",
            cancellationToken);
    }

    [HttpPost("{id:guid}/itinerary/items/from-venue")]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> AddRecommendedItineraryItem(
        Guid id,
        [FromBody] AddRecommendedItineraryItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddRecommendedItineraryItemCommand(
            id,
            request.DayId,
            request.Category,
            request.PlaceId,
            request.OrderIndex);

        return await SendItineraryMutation(
            command,
            "Recommended itinerary item added successfully.",
            "Failed to add the recommended itinerary item.",
            cancellationToken);
    }

    [HttpPut("{id:guid}/itinerary/items/{itemId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateItineraryItem(
        Guid id,
        Guid itemId,
        [FromBody] UpdateItineraryItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateItineraryItemCommand(
            id,
            itemId,
            request.Title,
            request.Description,
            request.StartTime,
            request.EndTime,
            request.Location,
            request.Category,
            request.EstimatedCost);

        return await SendItineraryMutation(
            command,
            "Itinerary item updated successfully.",
            "Failed to update itinerary item.",
            cancellationToken);
    }

    [HttpPut("{id:guid}/itinerary/items/{itemId:guid}/position")]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> MoveItineraryItem(
        Guid id,
        Guid itemId,
        [FromBody] MoveItineraryItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new MoveItineraryItemCommand(
            id,
            itemId,
            request.TargetDayId,
            request.TargetOrderIndex);

        return await SendItineraryMutation(
            command,
            "Itinerary item moved successfully.",
            "Failed to move itinerary item.",
            cancellationToken);
    }

    [HttpDelete("{id:guid}/itinerary/items/{itemId:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<ItineraryResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteItineraryItem(
        Guid id,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteItineraryItemCommand(id, itemId);

        return await SendItineraryMutation(
            command,
            "Itinerary item deleted successfully.",
            "Failed to delete itinerary item.",
            cancellationToken);
    }

    [HttpGet("{id:guid}/itinerary/items/{itemId:guid}/venues")]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<VenueSuggestionResponse>>),
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
            return Failure<IReadOnlyList<VenueSuggestionResponse>>(
                result.Error,
                "Failed to load venue suggestions.");
        }

        return Ok(ApiResponse<IReadOnlyList<VenueSuggestionResponse>>
            .SuccessResponse(
                result.Value!,
                "Venue suggestions loaded successfully.",
                StatusCodes.Status200OK));
    }

    [HttpGet("{id:guid}/itinerary/days/{dayId:guid}/venues")]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<VenueSuggestionResponse>>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDayVenueSuggestions(
        Guid id,
        Guid dayId,
        [FromQuery] string category,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetDayVenueSuggestionsQuery(id, dayId, category),
            cancellationToken);

        if (result.IsFailure)
        {
            return Failure<IReadOnlyList<VenueSuggestionResponse>>(
                result.Error,
                "Failed to load recommendations for this day.");
        }

        return Ok(ApiResponse<IReadOnlyList<VenueSuggestionResponse>>
            .SuccessResponse(
                result.Value!,
                "Recommendations loaded successfully.",
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
            request.PlaceId);

        return await SendItineraryMutation(
            command,
            "Venue selected successfully.",
            "Failed to select the venue.",
            cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTrip(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeleteTripCommand(id),
            cancellationToken);

        if (result.IsFailure)
        {
            return Failure<object>(
                result.Error,
                "Failed to delete trip.");
        }

        return NoContent();
    }

    private async Task<IActionResult> SendItineraryMutation<TCommand>(
        TCommand command,
        string successMessage,
        string failureMessage,
        CancellationToken cancellationToken)
        where TCommand : IRequest<
            Safarni.Application.Common.Results.Result<ItineraryResponse>>
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Failure<ItineraryResponse>(
                result.Error,
                failureMessage);
        }

        return Ok(ApiResponse<ItineraryResponse>.SuccessResponse(
            result.Value!,
            successMessage,
            StatusCodes.Status200OK));
    }

    private ObjectResult Failure<T>(Error error, string message)
    {
        var statusCode = MapStatusCode(error.Type);
        var response = ApiResponse<T>.FailureResponse(
            [new ApiError(error.Code, error.Message)],
            message,
            statusCode);

        return StatusCode(statusCode, response);
    }

    private static int MapStatusCode(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}

public sealed record UpdateTripRequest(
    string Name,
    string Destination,
    DateTime StartDate,
    DateTime EndDate,
    string OriginCountry,
    IReadOnlyList<TripCitySelection> Cities,
    string? OriginCountryCode = null,
    string? OriginCity = null,
    string? OriginAirportCode = null,
    string? OriginAirportName = null);

public sealed record GenerateItineraryRequest(
    GenerateItineraryPreferences Preferences,
    decimal? BudgetAmount,
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

public sealed record AddRecommendedItineraryItemRequest(
    Guid DayId,
    string Category,
    string PlaceId,
    int? OrderIndex);

public sealed record UpdateItineraryItemRequest(
    string Title,
    string? Description,
    string? StartTime,
    string? EndTime,
    string? Location,
    string Category,
    decimal? EstimatedCost);

public sealed record MoveItineraryItemRequest(
    Guid TargetDayId,
    int TargetOrderIndex);

public sealed record SelectVenueRequest(string PlaceId);
