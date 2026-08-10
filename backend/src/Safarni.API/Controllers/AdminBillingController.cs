using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Controllers.Admin;
using Safarni.API.Extensions;
using Safarni.Application.Features.Admin.Billing;
using Safarni.Application.Features.Billing;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminOrAnalyst)]
[Route("api/admin/billing")]
public sealed class AdminBillingController : AdminControllerBase
{
    private readonly ISender _sender;

    public AdminBillingController(ISender sender) => _sender = sender;

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAdminBillingOverviewQuery(), cancellationToken);
        return result.IsFailure
            ? Failure<AdminBillingOverviewResponse>(result.Error, "Failed to load billing overview.")
            : Ok(ApiResponse<AdminBillingOverviewResponse>.SuccessResponse(
                result.Value!, "Billing overview loaded.", StatusCodes.Status200OK));
    }

    [HttpPut("plans/{planId:guid}")]
    [Authorize(Policy = AdminPolicies.AdminOnly)]
    public async Task<IActionResult> UpdatePlan(
        Guid planId,
        [FromBody] UpdatePricingPlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdatePricingPlanCommand(planId, request.Price, request.CurrencyCode,
                request.AiItineraryGenerationsPerPeriod, request.MaxPublishedPackages, request.IsActive),
            cancellationToken);

        return result.IsFailure
            ? Failure<PricingPlanResponse>(result.Error, "Pricing plan update failed.")
            : Ok(ApiResponse<PricingPlanResponse>.SuccessResponse(
                result.Value!, "Pricing plan updated.", StatusCodes.Status200OK));
    }

    public sealed record UpdatePricingPlanRequest(
        decimal Price,
        string CurrencyCode,
        int AiItineraryGenerationsPerPeriod,
        int MaxPublishedPackages,
        bool IsActive);
}
