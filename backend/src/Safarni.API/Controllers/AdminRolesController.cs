using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Controllers.Admin;
using Safarni.API.Extensions;
using Safarni.Application.Features.Admin.Roles;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminOnly)]
[Route("api/admin/roles")]
public sealed class AdminRolesController : AdminControllerBase
{
    private readonly ISender _sender;

    public AdminRolesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRolesQuery(), cancellationToken);
        if (result.IsFailure)
        {
            return Failure<IReadOnlyList<RoleResponse>>(result.Error, "Failed to load roles.");
        }

        return Ok(ApiResponse<IReadOnlyList<RoleResponse>>.SuccessResponse(
            result.Value!,
            "Roles loaded.",
            StatusCodes.Status200OK));
    }
}
