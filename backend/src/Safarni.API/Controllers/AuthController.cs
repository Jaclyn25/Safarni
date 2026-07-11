using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Auth.Login;
using Safarni.Application.Features.Auth.Logout;
using Safarni.Application.Features.Auth.RefreshTokens;
using Safarni.Application.Features.Auth.Register;

namespace Safarni.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var statusCode = MapStatusCode(result.Error.Type);
            var failureResponse = ApiResponse<RegisterUserResponse>.FailureResponse(
                [new ApiError(result.Error.Code, result.Error.Message)],
                "Registration failed.",
                statusCode);

            return StatusCode(statusCode, failureResponse);
        }

        var successResponse = ApiResponse<RegisterUserResponse>.SuccessResponse(
            result.Value!,
            "User registered successfully.",
            StatusCodes.Status201Created);

        return StatusCode(StatusCodes.Status201Created, successResponse);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var statusCode = MapStatusCode(result.Error.Type);
            var failureResponse = ApiResponse<LoginResponse>.FailureResponse(
                [new ApiError(result.Error.Code, result.Error.Message)],
                "Login failed.",
                statusCode);

            return StatusCode(statusCode, failureResponse);
        }

        var successResponse = ApiResponse<LoginResponse>.SuccessResponse(
            result.Value!,
            "Login successful.",
            StatusCodes.Status200OK);

        return Ok(successResponse);
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var statusCode = MapStatusCode(result.Error.Type);
            var failureResponse = ApiResponse<RefreshTokenResponse>.FailureResponse(
                [new ApiError(result.Error.Code, result.Error.Message)],
                "Refresh token failed.",
                statusCode);

            return StatusCode(statusCode, failureResponse);
        }

        var successResponse = ApiResponse<RefreshTokenResponse>.SuccessResponse(
            result.Value!,
            "Token refreshed successfully.",
            StatusCodes.Status200OK);

        return Ok(successResponse);
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<LogoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LogoutResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<LogoutResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<LogoutResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var statusCode = MapStatusCode(result.Error.Type);
            var failureResponse = ApiResponse<LogoutResponse>.FailureResponse(
                [new ApiError(result.Error.Code, result.Error.Message)],
                "Logout failed.",
                statusCode);

            return StatusCode(statusCode, failureResponse);
        }

        var successResponse = ApiResponse<LogoutResponse>.SuccessResponse(
            result.Value!,
            "Logout successful.",
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
}
