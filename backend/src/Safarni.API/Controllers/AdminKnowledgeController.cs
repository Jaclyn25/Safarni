using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Controllers.Admin;
using Safarni.API.Extensions;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Common;
using Safarni.Application.Features.Admin.Knowledge;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminOrContentManager)]
[Route("api/admin/knowledge")]
public sealed class AdminKnowledgeController : AdminControllerBase
{
    private readonly ISender _sender;

    public AdminKnowledgeController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetKnowledge(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] bool? isArchived,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var criteria = new KnowledgeSearchCriteria(
            search,
            category,
            isArchived,
            page,
            pageSize);

        var result = await _sender.Send(
            new ListKnowledgeQuery(criteria),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<PagedResult<AdminKnowledgeResponse>>(result.Error, "Failed to load knowledge items.");
        }

        return Ok(ApiResponse<PagedResult<AdminKnowledgeResponse>>.SuccessResponse(
            result.Value!,
            "Knowledge items loaded.",
            StatusCodes.Status200OK));
    }

    [HttpPost]
    public async Task<IActionResult> CreateKnowledge(
        [FromBody] KnowledgeInput input,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateKnowledgeCommand(input),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<AdminKnowledgeResponse>(result.Error, "Failed to create knowledge item.");
        }

        return CreatedAtAction(
            nameof(GetKnowledge),
            new { },
            ApiResponse<AdminKnowledgeResponse>.SuccessResponse(
                result.Value!,
                "Knowledge item created.",
                StatusCodes.Status201Created));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateKnowledge(
        int id,
        [FromBody] KnowledgeInput input,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateKnowledgeCommand(id, input),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<AdminKnowledgeResponse>(result.Error, "Failed to update knowledge item.");
        }

        return Ok(ApiResponse<AdminKnowledgeResponse>.SuccessResponse(
            result.Value!,
            "Knowledge item updated.",
            StatusCodes.Status200OK));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> ArchiveKnowledge(int id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ArchiveKnowledgeCommand(id, Archived: true),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<AdminKnowledgeResponse>(result.Error, "Failed to archive knowledge item.");
        }

        return Ok(ApiResponse<AdminKnowledgeResponse>.SuccessResponse(
            result.Value!,
            "Knowledge item archived.",
            StatusCodes.Status200OK));
    }

    [HttpPost("{id:int}/restore")]
    public async Task<IActionResult> RestoreKnowledge(int id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ArchiveKnowledgeCommand(id, Archived: false),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<AdminKnowledgeResponse>(result.Error, "Failed to restore knowledge item.");
        }

        return Ok(ApiResponse<AdminKnowledgeResponse>.SuccessResponse(
            result.Value!,
            "Knowledge item restored.",
            StatusCodes.Status200OK));
    }

    [HttpPost("reindex")]
    public async Task<IActionResult> Reindex(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ReindexKnowledgeCommand(),
            cancellationToken);
        if (result.IsFailure)
        {
            return Failure<ReindexKnowledgeResponse>(result.Error, "Reindex could not start.");
        }

        return Ok(ApiResponse<ReindexKnowledgeResponse>.SuccessResponse(
            result.Value!,
            result.Value!.Success
                ? "Reindex completed."
                : "Reindex did not complete.",
            StatusCodes.Status200OK));
    }
}
