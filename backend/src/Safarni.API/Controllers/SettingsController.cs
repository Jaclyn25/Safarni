using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Extensions;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Settings.GetMySettings;
using Safarni.Application.Features.Settings.UpdateMySettings;

namespace Safarni.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class SettingsController : ControllerBase
{
    private readonly ISender _sender;

    public SettingsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<GetMySettingsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GetMySettingsResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<GetMySettingsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMySettings(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var result = await _sender.Send(new GetMySettingsQuery(userId), cancellationToken);

        if (result.IsFailure)
        {
            var statusCode = MapStatusCode(result.Error.Type);
            var failureResponse = ApiResponse<GetMySettingsResponse>.FailureResponse(
                [new ApiError(result.Error.Code, result.Error.Message)],
                "Get settings failed.",
                statusCode);

            return StatusCode(statusCode, failureResponse);
        }

        var successResponse = ApiResponse<GetMySettingsResponse>.SuccessResponse(
            result.Value!,
            "Settings returned successfully.",
            StatusCodes.Status200OK);

        return Ok(successResponse);
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(ApiResponse<UpdateMySettingsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UpdateMySettingsResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UpdateMySettingsResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<UpdateMySettingsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMySettings([FromBody] UpdateMySettingsRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var command = new UpdateMySettingsCommand(
            userId,
            request.EmailNotificationsEnabled,
            request.PushNotificationsEnabled,
            request.ProfileIsPrivate,
            request.ShowLastSeen,
            request.ShowEmail,
            request.ShowPhoneNumber);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var statusCode = MapStatusCode(result.Error.Type);
            var failureResponse = ApiResponse<UpdateMySettingsResponse>.FailureResponse(
                [new ApiError(result.Error.Code, result.Error.Message)],
                "Update settings failed.",
                statusCode);

            return StatusCode(statusCode, failureResponse);
        }

        var successResponse = ApiResponse<UpdateMySettingsResponse>.SuccessResponse(
            result.Value!,
            "Settings updated successfully.",
            StatusCodes.Status200OK);

        return Ok(successResponse);
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

    public sealed record UpdateMySettingsRequest(
        bool EmailNotificationsEnabled,
        bool PushNotificationsEnabled,
        bool ProfileIsPrivate,
        bool ShowLastSeen,
        bool ShowEmail,
        bool ShowPhoneNumber);
}
