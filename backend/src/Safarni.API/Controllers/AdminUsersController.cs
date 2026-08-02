using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Controllers.Admin;
using Safarni.API.Extensions;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Common;
using Safarni.Application.Features.Admin.Users;
using Safarni.Domain.Enums;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminOrSupport)]
[Route("api/admin/users")]
public sealed class AdminUsersController : AdminControllerBase
{
    private readonly ISender _sender;

    public AdminUsersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] UserRole? role,
        [FromQuery] bool? isEmailVerified,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDirection,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var criteria = new UserSearchCriteria(
            search,
            isActive,
            role,
            isEmailVerified,
            createdFrom,
            createdTo,
            sortBy,
            sortDirection,
            page,
            pageSize);

        var result = await _sender.Send(
            new ListAdminUsersQuery(criteria),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<PagedResult<AdminUserSummaryResponse>>(result.Error, "Failed to load users.");
        }

        return Ok(ApiResponse<PagedResult<AdminUserSummaryResponse>>.SuccessResponse(
            result.Value!,
            "Users loaded.",
            StatusCodes.Status200OK));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAdminUserQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            return Failure<AdminUserDetailResponse>(result.Error, "Failed to load user.");
        }

        return Ok(ApiResponse<AdminUserDetailResponse>.SuccessResponse(
            result.Value!,
            "User loaded.",
            StatusCodes.Status200OK));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = AdminPolicies.AdminOnly)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateUserStatusCommand(id, request.IsActive),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<AdminUserDetailResponse>(result.Error, "Failed to update user status.");
        }

        return Ok(ApiResponse<AdminUserDetailResponse>.SuccessResponse(
            result.Value!,
            "User status updated.",
            StatusCodes.Status200OK));
    }

    [HttpPost("{id:guid}/verify-email")]
    [Authorize(Policy = AdminPolicies.AdminOnly)]
    public async Task<IActionResult> VerifyEmail(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new VerifyUserEmailCommand(id),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<AdminUserDetailResponse>(result.Error, "Failed to verify email.");
        }

        return Ok(ApiResponse<AdminUserDetailResponse>.SuccessResponse(
            result.Value!,
            "Email marked as verified.",
            StatusCodes.Status200OK));
    }

    [HttpPost("{id:guid}/force-password-reset")]
    public async Task<IActionResult> ForcePasswordReset(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ForcePasswordResetCommand(id),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<ForcePasswordResetResponse>(result.Error, "Failed to force password reset.");
        }

        return Ok(ApiResponse<ForcePasswordResetResponse>.SuccessResponse(
            result.Value!,
            "Password reset initiated.",
            StatusCodes.Status200OK));
    }

    [HttpPut("{id:guid}/roles")]
    [Authorize(Policy = AdminPolicies.AdminOnly)]
    public async Task<IActionResult> UpdateRoles(
        Guid id,
        [FromBody] UpdateUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateUserRolesCommand(id, request.Role),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<AdminUserDetailResponse>(result.Error, "Failed to update roles.");
        }

        return Ok(ApiResponse<AdminUserDetailResponse>.SuccessResponse(
            result.Value!,
            "Roles updated.",
            StatusCodes.Status200OK));
    }
}

public sealed record UpdateUserStatusRequest(bool IsActive);
public sealed record UpdateUserRolesRequest(UserRole Role);
