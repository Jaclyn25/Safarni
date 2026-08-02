using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Controllers.Admin;
using Safarni.API.Extensions;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.AuditLogs;
using Safarni.Application.Features.Admin.Common;
using Safarni.Domain.Enums;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminOrSupport)]
[Route("api/admin/audit-logs")]
public sealed class AdminAuditLogsController : AdminControllerBase
{
    private readonly ISender _sender;

    public AdminAuditLogsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? search,
        [FromQuery] string? action,
        [FromQuery] string? targetType,
        [FromQuery] Guid? actorUserId,
        [FromQuery] AuditSeverity? severity,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var criteria = new AuditLogCriteria(
            search,
            action,
            targetType,
            actorUserId,
            severity,
            from,
            to,
            page,
            pageSize);

        var result = await _sender.Send(
            new ListAuditLogsQuery(criteria),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<PagedResult<AuditLogResponse>>(result.Error, "Failed to load audit logs.");
        }

        return Ok(ApiResponse<PagedResult<AuditLogResponse>>.SuccessResponse(
            result.Value!,
            "Audit logs loaded.",
            StatusCodes.Status200OK));
    }
}
