using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Controllers.Admin;
using Safarni.API.Extensions;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Agencies;
using Safarni.Application.Features.Agencies.Common;
using Safarni.Application.Features.Agencies.Documents;
using Safarni.Application.Features.Packages;
using Safarni.Domain.Enums;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize(Policy = AdminPolicies.AdminOrSupport)]
[Route("api/admin/agencies")]
public sealed class AdminAgenciesController : AdminControllerBase
{
    private readonly ISender _sender;

    public AdminAgenciesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] AgencyVerificationStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new ListAdminAgenciesQuery(status, search, page, pageSize),
            cancellationToken);

        return result.IsFailure
            ? Failure<PagedResult<AdminAgencySummaryResponse>>(result.Error, "Failed to load agencies.")
            : Ok(ApiResponse<PagedResult<AdminAgencySummaryResponse>>.SuccessResponse(
                result.Value!,
                "Agencies loaded.",
                StatusCodes.Status200OK));
    }

    [HttpGet("{agencyId:guid}")]
    public async Task<IActionResult> Get(Guid agencyId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAdminAgencyQuery(agencyId), cancellationToken);
        return result.IsFailure
            ? Failure<AdminAgencyDetailResponse>(result.Error, "Failed to load agency.")
            : Ok(ApiResponse<AdminAgencyDetailResponse>.SuccessResponse(
                result.Value!,
                "Agency loaded.",
                StatusCodes.Status200OK));
    }

    [HttpGet("{agencyId:guid}/packages")]
    public async Task<IActionResult> Packages(Guid agencyId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListAdminAgencyPackagesQuery(agencyId), cancellationToken);
        return result.IsFailure
            ? Failure<IReadOnlyList<TravelPackageResponse>>(result.Error, "Failed to load agency packages.")
            : Ok(ApiResponse<IReadOnlyList<TravelPackageResponse>>.SuccessResponse(result.Value!, "Agency packages loaded.", StatusCodes.Status200OK));
    }

    [HttpGet("{agencyId:guid}/verification-documents/{documentType}/content")]
    public async Task<IActionResult> DownloadDocument(
        Guid agencyId,
        AgencyVerificationDocumentType documentType,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetAgencyVerificationDocumentContentQuery(
                User.GetRequiredUserId(),
                agencyId,
                documentType,
                IsAuthorizedAdmin: true),
            cancellationToken);

        return result.IsFailure
            ? Failure<AgencyVerificationDocumentContentResponse>(
                result.Error,
                "Failed to open verification document.")
            : File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }

    [Authorize(Policy = AdminPolicies.AdminOnly)]
    [HttpPost("{agencyId:guid}/verification-documents/{documentType}/decision")]
    public async Task<IActionResult> DecideDocument(
        Guid agencyId,
        AgencyVerificationDocumentType documentType,
        [FromBody] DecideAgencyDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DecideAgencyDocumentCommand(
                agencyId,
                User.GetRequiredUserId(),
                documentType,
                request.Decision,
                request.Reason),
            cancellationToken);

        return result.IsFailure
            ? Failure<AdminAgencyDetailResponse>(result.Error, "Document review decision failed.")
            : Ok(ApiResponse<AdminAgencyDetailResponse>.SuccessResponse(
                result.Value!,
                "Document review decision saved.",
                StatusCodes.Status200OK));
    }

    [Authorize(Policy = AdminPolicies.AdminOnly)]
    [HttpPost("{agencyId:guid}/decision")]
    public async Task<IActionResult> Decide(
        Guid agencyId,
        [FromBody] DecideAgencyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DecideAgencyVerificationCommand(
                agencyId,
                User.GetRequiredUserId(),
                request.Decision,
                request.Reason),
            cancellationToken);

        return result.IsFailure
            ? Failure<AdminAgencyDetailResponse>(result.Error, "Agency verification decision failed.")
            : Ok(ApiResponse<AdminAgencyDetailResponse>.SuccessResponse(
                result.Value!,
                "Agency verification decision saved.",
                StatusCodes.Status200OK));
    }

    public sealed record DecideAgencyRequest(
        AgencyVerificationDecision Decision,
        string? Reason);

    public sealed record DecideAgencyDocumentRequest(
        AgencyDocumentDecision Decision,
        string? Reason);
}
