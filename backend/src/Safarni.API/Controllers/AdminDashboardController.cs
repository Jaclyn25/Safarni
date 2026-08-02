using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Controllers.Admin;
using Safarni.API.Extensions;
using Safarni.Application.Features.Admin.Dashboard;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminOrAnalyst)]
[Route("api/admin/dashboard")]
public sealed class AdminDashboardController : AdminControllerBase
{
    private readonly ISender _sender;

    public AdminDashboardController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDashboardSummaryQuery(), cancellationToken);
        if (result.IsFailure)
        {
            return Failure<DashboardSummaryResponse>(result.Error, "Failed to load dashboard summary.");
        }

        return Ok(ApiResponse<DashboardSummaryResponse>.SuccessResponse(
            result.Value!,
            "Dashboard summary loaded.",
            StatusCodes.Status200OK));
    }

    [HttpGet("recent-activity")]
    public async Task<IActionResult> GetRecentActivity(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetRecentActivityQuery(limit),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<IReadOnlyList<RecentActivityResponse>>(result.Error, "Failed to load recent activity.");
        }

        return Ok(ApiResponse<IReadOnlyList<RecentActivityResponse>>.SuccessResponse(
            result.Value!,
            "Recent activity loaded.",
            StatusCodes.Status200OK));
    }
}
