using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Controllers.Admin;
using Safarni.API.Extensions;
using Safarni.Application.Features.Admin.Analytics;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminOrAnalyst)]
[Route("api/admin/analytics")]
public sealed class AdminAnalyticsController : AdminControllerBase
{
    private readonly ISender _sender;

    public AdminAnalyticsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetAnalyticsOverviewQuery(from, to),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<AnalyticsOverviewResponse>(result.Error, "Failed to load analytics overview.");
        }

        return Ok(ApiResponse<AnalyticsOverviewResponse>.SuccessResponse(
            result.Value!,
            "Analytics overview loaded.",
            StatusCodes.Status200OK));
    }

    [HttpGet("funnels")]
    public async Task<IActionResult> GetFunnels(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetAnalyticsFunnelsQuery(),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<AnalyticsFunnelResponse>(result.Error, "Failed to load funnels.");
        }

        return Ok(ApiResponse<AnalyticsFunnelResponse>.SuccessResponse(
            result.Value!,
            "Funnels loaded.",
            StatusCodes.Status200OK));
    }

    [HttpGet("top-destinations")]
    public async Task<IActionResult> GetTopDestinations(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetTopDestinationsQuery(limit),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<IReadOnlyList<TopDestinationResponse>>(result.Error, "Failed to load top destinations.");
        }

        return Ok(ApiResponse<IReadOnlyList<TopDestinationResponse>>.SuccessResponse(
            result.Value!,
            "Top destinations loaded.",
            StatusCodes.Status200OK));
    }
}
