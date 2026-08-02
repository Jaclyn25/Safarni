using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Controllers.Admin;
using Safarni.API.Extensions;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Ai;
using Safarni.Application.Features.Admin.Common;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminOrSupport)]
[Route("api/admin/ai")]
public sealed class AdminAiController : AdminControllerBase
{
    private readonly ISender _sender;

    public AdminAiController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity(
        [FromQuery] Guid? userId,
        [FromQuery] string? provider,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var criteria = new AiActivityCriteria(
            userId,
            provider,
            status,
            from,
            to,
            page,
            pageSize);

        var result = await _sender.Send(
            new ListAiActivityQuery(criteria),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<PagedResult<AiActivityResponse>>(result.Error, "Failed to load AI activity.");
        }

        return Ok(ApiResponse<PagedResult<AiActivityResponse>>.SuccessResponse(
            result.Value!,
            "AI activity loaded.",
            StatusCodes.Status200OK));
    }

    [HttpGet("conversations/{id:guid}")]
    public async Task<IActionResult> GetConversation(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetAiConversationQuery(id),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<AdminConversationResponse>(result.Error, "Failed to load conversation.");
        }

        return Ok(ApiResponse<AdminConversationResponse>.SuccessResponse(
            result.Value!,
            "Conversation loaded.",
            StatusCodes.Status200OK));
    }
}
