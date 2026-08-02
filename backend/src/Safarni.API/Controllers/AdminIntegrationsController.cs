using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Controllers.Admin;
using Safarni.API.Extensions;
using Safarni.Application.Abstractions.Integrations;
using Safarni.Application.Features.Admin.Integrations;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminOnly)]
[Route("api/admin/integrations")]
public sealed class AdminIntegrationsController : AdminControllerBase
{
    private readonly ISender _sender;

    public AdminIntegrationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetIntegrationsHealthQuery(),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<IReadOnlyList<IntegrationHealthCheck>>(result.Error, "Failed to check integrations.");
        }

        return Ok(ApiResponse<IReadOnlyList<IntegrationHealthCheck>>.SuccessResponse(
            result.Value!,
            "Integration health checked.",
            StatusCodes.Status200OK));
    }

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetIntegrationsConfigQuery(),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<IReadOnlyList<IntegrationConfigEntry>>(result.Error, "Failed to load configuration status.");
        }

        return Ok(ApiResponse<IReadOnlyList<IntegrationConfigEntry>>.SuccessResponse(
            result.Value!,
            "Configuration status loaded.",
            StatusCodes.Status200OK));
    }

    [HttpPost("{name}/test")]
    public async Task<IActionResult> TestIntegration(string name, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new TestIntegrationCommand(name),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<IntegrationHealthCheck>(result.Error, "Failed to test integration.");
        }

        return Ok(ApiResponse<IntegrationHealthCheck>.SuccessResponse(
            result.Value!,
            "Integration test completed.",
            StatusCodes.Status200OK));
    }
}
