using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Extensions;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Profile.GetMyProfile;
using Safarni.Application.Features.Profile.UpdateMyProfile;
using Safarni.Application.Features.Profile.UploadProfilePhoto;

namespace Safarni.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class ProfileController : ControllerBase
{
    private const long MaxProfilePhotoSizeBytes = 5 * 1024 * 1024;

    private readonly ISender _sender;

    public ProfileController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<GetMyProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GetMyProfileResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<GetMyProfileResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var result = await _sender.Send(new GetMyProfileQuery(userId), cancellationToken);

        if (result.IsFailure)
        {
            var statusCode = MapStatusCode(result.Error.Type);
            var failureResponse = ApiResponse<GetMyProfileResponse>.FailureResponse(
                [new ApiError(result.Error.Code, result.Error.Message)],
                "Get profile failed.",
                statusCode);

            return StatusCode(statusCode, failureResponse);
        }

        var successResponse = ApiResponse<GetMyProfileResponse>.SuccessResponse(
            result.Value!,
            "Profile returned successfully.",
            StatusCodes.Status200OK);

        return Ok(successResponse);
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(ApiResponse<UpdateMyProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UpdateMyProfileResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UpdateMyProfileResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<UpdateMyProfileResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var command = new UpdateMyProfileCommand(
            userId,
            request.FullName,
            request.PhoneNumber,
            request.DateOfBirth,
            request.Country,
            request.City,
            request.Bio);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var statusCode = MapStatusCode(result.Error.Type);
            var failureResponse = ApiResponse<UpdateMyProfileResponse>.FailureResponse(
                [new ApiError(result.Error.Code, result.Error.Message)],
                "Update profile failed.",
                statusCode);

            return StatusCode(statusCode, failureResponse);
        }

        var successResponse = ApiResponse<UpdateMyProfileResponse>.SuccessResponse(
            result.Value!,
            "Profile updated successfully.",
            StatusCodes.Status200OK);

        return Ok(successResponse);
    }

    [HttpPost("me/photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxProfilePhotoSizeBytes)]
    [ProducesResponseType(typeof(ApiResponse<UploadProfilePhotoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UploadProfilePhotoResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UploadProfilePhotoResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<UploadProfilePhotoResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadMyProfilePhoto([FromForm] UploadProfilePhotoRequest request, CancellationToken cancellationToken)
    {
        if (request.File is null)
        {
            var failureResponse = ApiResponse<UploadProfilePhotoResponse>.FailureResponse(
                [new ApiError("User.InvalidProfilePhoto", "Profile photo file is required.")],
                "Upload profile photo failed.",
                StatusCodes.Status400BadRequest);

            return BadRequest(failureResponse);
        }

        await using var memoryStream = new MemoryStream();
        await request.File.CopyToAsync(memoryStream, cancellationToken);

        var userId = User.GetRequiredUserId();
        var command = new UploadProfilePhotoCommand(
            userId,
            request.File.FileName,
            request.File.ContentType,
            request.File.Length,
            memoryStream.ToArray());

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var statusCode = MapStatusCode(result.Error.Type);
            var failureResponse = ApiResponse<UploadProfilePhotoResponse>.FailureResponse(
                [new ApiError(result.Error.Code, result.Error.Message)],
                "Upload profile photo failed.",
                statusCode);

            return StatusCode(statusCode, failureResponse);
        }

        var successResponse = ApiResponse<UploadProfilePhotoResponse>.SuccessResponse(
            result.Value!,
            "Profile photo uploaded successfully.",
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

    public sealed record UpdateMyProfileRequest(
        string FullName,
        string? PhoneNumber,
        DateTime? DateOfBirth,
        string? Country,
        string? City,
        string? Bio);

    public sealed record UploadProfilePhotoRequest(IFormFile File);
}
