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
using Safarni.Application.Features.Auth.ForgotPassword;
using System.Security.Claims;
using Safarni.Application.Features.Auth.ChangePassword;
using Safarni.Application.Features.Auth.VerifyEmail;
using Safarni.Application.Features.Auth.ResetPassword;
using Safarni.Application.Features.Auth.GoogleLogin;

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
            "Registration successful. A verification email has been sent.",
            StatusCodes.Status201Created);

        return StatusCode(StatusCodes.Status201Created, successResponse);
    }
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<ForgotPasswordResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var failureResponse = ApiResponse<ForgotPasswordResponse>.FailureResponse(
                [new ApiError(result.Error.Code, result.Error.Message)],
                "Forgot password failed.",
                StatusCodes.Status400BadRequest);

            return BadRequest(failureResponse);
        }

        var successResponse = ApiResponse<ForgotPasswordResponse>.SuccessResponse(
            result.Value!,
            "If the email exists, a reset link has been sent.",
            StatusCodes.Status200OK);

        return Ok(successResponse);
    }
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            var failureResponse = ApiResponse<ResetPasswordResponse>.FailureResponse(
                [new ApiError(result.Error.Code, result.Error.Message)],
                "Password reset failed.",
                StatusCodes.Status400BadRequest);
            return BadRequest(failureResponse);
        }
        var successResponse = ApiResponse<ResetPasswordResponse>.SuccessResponse(
            result.Value!,
            "Password reset successful.",
            StatusCodes.Status200OK);
        return Ok(successResponse);
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
    [HttpPost("google-login")]
[ProducesResponseType(
    typeof(ApiResponse<LoginResponse>),
    StatusCodes.Status200OK)]
[ProducesResponseType(
    typeof(ApiResponse<LoginResponse>),
    StatusCodes.Status400BadRequest)]
[ProducesResponseType(
    typeof(ApiResponse<LoginResponse>),
    StatusCodes.Status401Unauthorized)]
[ProducesResponseType(
    typeof(ApiResponse<LoginResponse>),
    StatusCodes.Status403Forbidden)]
public async Task<IActionResult> GoogleLogin(
    [FromBody] GoogleLoginCommand command,
    CancellationToken cancellationToken)
{
    var result =
        await _sender.Send(
            command,
            cancellationToken);

    if (result.IsFailure)
    {
        var statusCode =
            MapStatusCode(result.Error.Type);

        var failureResponse =
            ApiResponse<LoginResponse>.FailureResponse(
                [
                    new ApiError(
                        result.Error.Code,
                        result.Error.Message)
                ],
                "Google login failed.",
                statusCode);

        return StatusCode(
            statusCode,
            failureResponse);
    }

    var successResponse =
        ApiResponse<LoginResponse>.SuccessResponse(
            result.Value!,
            "Google login successful.",
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


    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(ApiResponse<VerifyEmailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VerifyEmailResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<VerifyEmailResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<VerifyEmailResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var statusCode = MapStatusCode(result.Error.Type);
            var failureResponse = ApiResponse<VerifyEmailResponse>.FailureResponse(
                [new ApiError(result.Error.Code, result.Error.Message)],
                "Email verification failed.",
                statusCode);

            return StatusCode(statusCode, failureResponse);
        }

        var successResponse = ApiResponse<VerifyEmailResponse>.SuccessResponse(
            result.Value!,
            "Email verified successfully.",
            StatusCodes.Status200OK);

        return Ok(successResponse);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await _sender.Send(command with { UserId = userId }, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
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
