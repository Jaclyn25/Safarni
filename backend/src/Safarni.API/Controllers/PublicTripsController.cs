using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.PublicTrips.Commands;
using Safarni.Application.Features.PublicTrips.Queries;
using Safarni.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize]
[Route("api/public-trips")]
public sealed class PublicTripsController : ControllerBase
{
    private readonly ISender _sender;

    public PublicTripsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<PublicTripSearchResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchPublicTrips(
        [FromQuery] string? destination,
        [FromQuery] int? minDuration,
        [FromQuery] int? maxDuration,
        [FromQuery] decimal? maxBudget,
        [FromQuery] string? sortOrder,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SearchPublicTripsQuery(destination, minDuration, maxDuration, maxBudget, sortOrder), cancellationToken);

        if (result.IsFailure)
        {
            return Failure<IReadOnlyCollection<PublicTripSearchResponse>>(result.Error, "Failed to search public trips.");
        }

        return Ok(ApiResponse<IReadOnlyCollection<PublicTripSearchResponse>>.SuccessResponse(
            result.Value!, "Public trips searched successfully.", StatusCodes.Status200OK));
    }

    [HttpGet("{versionId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PublicTripDetailsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicTripDetails(
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPublicTripDetailsQuery(versionId), cancellationToken);

        if (result.IsFailure)
        {
            return Failure<PublicTripDetailsResponse>(result.Error, "Failed to load public trip.");
        }

        return Ok(ApiResponse<PublicTripDetailsResponse>.SuccessResponse(
            result.Value!, "Public trip loaded successfully.", StatusCodes.Status200OK));
    }

    [HttpPost("{versionId:guid}/like")]
    [ProducesResponseType(typeof(ApiResponse<ToggleTripInteractionResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleLike(
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ToggleTripInteractionCommand(versionId, TripInteractionType.Like), cancellationToken);

        if (result.IsFailure)
        {
            return Failure<ToggleTripInteractionResult>(result.Error, "Failed to like trip.");
        }

        return Ok(ApiResponse<ToggleTripInteractionResult>.SuccessResponse(
            result.Value!, "Like toggled successfully.", StatusCodes.Status200OK));
    }

    [HttpPost("{versionId:guid}/save")]
    [ProducesResponseType(typeof(ApiResponse<ToggleTripInteractionResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleSave(
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ToggleTripInteractionCommand(versionId, TripInteractionType.Save), cancellationToken);

        if (result.IsFailure)
        {
            return Failure<ToggleTripInteractionResult>(result.Error, "Failed to save trip.");
        }

        return Ok(ApiResponse<ToggleTripInteractionResult>.SuccessResponse(
            result.Value!, "Save toggled successfully.", StatusCodes.Status200OK));
    }

    [HttpPost("{versionId:guid}/copy")]
    [ProducesResponseType(typeof(ApiResponse<CopyPublicTripResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CopyTrip(
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CopyPublicTripCommand(versionId), cancellationToken);

        if (result.IsFailure)
        {
            return Failure<CopyPublicTripResult>(result.Error, "Failed to copy trip.");
        }

        return Ok(ApiResponse<CopyPublicTripResult>.SuccessResponse(
            result.Value!, "Trip copied successfully.", StatusCodes.Status200OK));
    }

    [HttpPost("{versionId:guid}/rate")]
    [ProducesResponseType(typeof(ApiResponse<RatePublicTripResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RateTrip(
        Guid versionId,
        [FromBody] RateTripRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RatePublicTripCommand(versionId, request.Score, request.Comment), cancellationToken);

        if (result.IsFailure)
        {
            return Failure<RatePublicTripResult>(result.Error, "Failed to rate trip.");
        }

        return Ok(ApiResponse<RatePublicTripResult>.SuccessResponse(
            result.Value!, "Trip rated successfully.", StatusCodes.Status200OK));
    }

    [HttpDelete("{versionId:guid}/rate")]
    [ProducesResponseType(typeof(ApiResponse<DeletePublicTripRatingResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteRateTrip(
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeletePublicTripRatingCommand(versionId), cancellationToken);

        if (result.IsFailure)
        {
            return Failure<DeletePublicTripRatingResult>(result.Error, "Failed to delete trip rating.");
        }

        return Ok(ApiResponse<DeletePublicTripRatingResult>.SuccessResponse(
            result.Value!, "Trip rating deleted successfully.", StatusCodes.Status200OK));
    }

    [HttpPost("{versionId:guid}/report")]
    [ProducesResponseType(typeof(ApiResponse<ReportPublicTripResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReportTrip(
        Guid versionId,
        [FromBody] ReportTripRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ReportPublicTripCommand(versionId, request.Reason), cancellationToken);

        if (result.IsFailure)
        {
            return Failure<ReportPublicTripResult>(result.Error, "Failed to report trip.");
        }

        return Ok(ApiResponse<ReportPublicTripResult>.SuccessResponse(
            result.Value!, "Trip reported successfully.", StatusCodes.Status200OK));
    }

    private ObjectResult Failure<T>(Error error, string message)
    {
        var statusCode = MapStatusCode(error.Type);
        var response = ApiResponse<T>.FailureResponse(
            new[] { new ApiError(error.Code, error.Message) },
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

public sealed record RateTripRequest(int Score, string? Comment);
public sealed record ReportTripRequest(string? Reason);