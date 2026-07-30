using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Trips;
using Safarni.Application.Features.Trips.CreateTrip;
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

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TripResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TripResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TripResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateTrip(
        [FromBody] CreateTripCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Failure<TripResponse>(result.Error, "Failed to create trip.");
        }

        return StatusCode(StatusCodes.Status201Created, ApiResponse<TripResponse>.SuccessResponse(
            result.Value!,
            "Trip created successfully.",
            StatusCodes.Status201Created));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TripResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TripResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TripResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TripResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<TripResponse>), StatusCodes.Status404NotFound)]
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
            request.EndDate);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Failure<TripResponse>(result.Error, "Failed to update trip.");
        }

        return Ok(ApiResponse<TripResponse>.SuccessResponse(
            result.Value!,
            "Trip updated successfully.",
            StatusCodes.Status200OK));
    }
    [HttpDelete("{id:guid}")]
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
    DateTime EndDate);
