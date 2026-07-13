using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Preferences;
using Safarni.Application.Features.Preferences.GetTravelProfile;
using Safarni.Application.Features.Preferences.UpsertTravelProfile;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize]
[Route("api/preferences")]
public sealed class PreferencesController : ControllerBase
{
    private readonly ISender _sender;

    public PreferencesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("travel")]
    [ProducesResponseType(typeof(ApiResponse<TravelProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TravelProfileResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TravelProfileResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTravelProfile(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTravelProfileQuery(), cancellationToken);

        if (result.IsFailure)
        {
            return Failure<TravelProfileResponse>(result.Error, "Travel profile lookup failed.");
        }

        return Ok(ApiResponse<TravelProfileResponse>.SuccessResponse(
            result.Value!,
            "Travel profile retrieved successfully.",
            StatusCodes.Status200OK));
    }

    [HttpPut("travel")]
    [ProducesResponseType(typeof(ApiResponse<TravelProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TravelProfileResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TravelProfileResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpsertTravelProfile(
        [FromBody] UpsertTravelProfileCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Failure<TravelProfileResponse>(result.Error, "Travel profile update failed.");
        }

        return Ok(ApiResponse<TravelProfileResponse>.SuccessResponse(
            result.Value!,
            "Travel profile saved successfully.",
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
