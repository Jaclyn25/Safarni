using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Safarni.API.Common;
using Safarni.API.Extensions;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Agencies.Common;
using Safarni.Application.Features.Agencies.Dashboard;
using Safarni.Application.Features.Agencies.Documents;
using Safarni.Application.Features.Agencies.Profile;
using Safarni.Application.Features.Agencies.Onboarding;
using Safarni.Application.Features.Agencies.Public;
using Safarni.Application.Features.Agencies.Verification;
using Safarni.Domain.Enums;

namespace Safarni.API.Controllers;

[ApiController]
[Authorize(Policy = AccountPolicies.TravelAgencyOnly)]
[Route("api/agency")]
public sealed class AgencyController : ControllerBase
{
    private const long MaxDocumentSizeBytes = 10 * 1024 * 1024;
    private const long MaxMultipartRequestSizeBytes = 11 * 1024 * 1024;
    private readonly ISender _sender;

    public AgencyController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("onboarding")]
    public async Task<IActionResult> GetOnboardingStatus(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetAgencyOnboardingStatusQuery(User.GetRequiredUserId()),
            cancellationToken);
        return Ok(ApiResponse<AgencyOnboardingStatusResponse>.SuccessResponse(
            result.Value!,
            "Agency onboarding status loaded.",
            StatusCodes.Status200OK));
    }

    [HttpPost("onboarding")]
    public async Task<IActionResult> CompleteOnboarding(
        [FromBody] CompleteAgencyOnboardingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CompleteAgencyOnboardingCommand(
                User.GetRequiredUserId(),
                request.CompanyName,
                request.Description,
                request.Address,
                request.City,
                request.Country,
                request.CompanyPhone,
                request.LogoUrl,
                request.WebsiteUrl),
            cancellationToken);

        return result.IsFailure
            ? Failure<AgencyProfileResponse>(result.Error, "Agency setup failed.")
            : StatusCode(
                StatusCodes.Status201Created,
                ApiResponse<AgencyProfileResponse>.SuccessResponse(
                    result.Value!,
                    "Company profile created. You can now submit verification documents.",
                    StatusCodes.Status201Created));
    }

    [HttpGet("verification")]
    public async Task<IActionResult> GetVerification(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetMyAgencyVerificationQuery(User.GetRequiredUserId()),
            cancellationToken);

        return result.IsFailure
            ? Failure<AgencyVerificationStatusResponse>(result.Error, "Failed to load agency verification status.")
            : Ok(ApiResponse<AgencyVerificationStatusResponse>.SuccessResponse(
                result.Value!,
                "Agency verification status loaded.",
                StatusCodes.Status200OK));
    }

    [HttpPost("verification-documents/{documentType}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxMultipartRequestSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxMultipartRequestSizeBytes)]
    public async Task<IActionResult> UploadVerificationDocument(
        AgencyVerificationDocumentType documentType,
        [FromForm] UploadAgencyVerificationDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length > MaxDocumentSizeBytes)
        {
            return Failure<AgencyVerificationStatusResponse>(
                TravelAgencyErrors.InvalidDocument,
                "Verification document upload failed.");
        }

        await using var memory = new MemoryStream();
        await request.File.CopyToAsync(memory, cancellationToken);

        var result = await _sender.Send(
            new UploadAgencyVerificationDocumentCommand(
                User.GetRequiredUserId(),
                documentType,
                request.DocumentNumber,
                request.ExpiryDate,
                request.File.FileName,
                request.File.ContentType,
                request.File.Length,
                memory.ToArray()),
            cancellationToken);

        return result.IsFailure
            ? Failure<AgencyVerificationStatusResponse>(result.Error, "Verification document upload failed.")
            : Ok(ApiResponse<AgencyVerificationStatusResponse>.SuccessResponse(
                result.Value!,
                "Verification document uploaded successfully.",
                StatusCodes.Status200OK));
    }

    [HttpDelete("verification-documents/{documentType}")]
    public async Task<IActionResult> DeleteVerificationDocument(
        AgencyVerificationDocumentType documentType,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeleteAgencyVerificationDocumentCommand(
                User.GetRequiredUserId(),
                documentType),
            cancellationToken);

        return result.IsFailure
            ? Failure<AgencyVerificationStatusResponse>(result.Error, "Verification document deletion failed.")
            : Ok(ApiResponse<AgencyVerificationStatusResponse>.SuccessResponse(
                result.Value!,
                "Verification document deleted.",
                StatusCodes.Status200OK));
    }

    [HttpGet("verification-documents/{documentType}/content")]
    public async Task<IActionResult> DownloadVerificationDocument(
        AgencyVerificationDocumentType documentType,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var status = await _sender.Send(new GetMyAgencyVerificationQuery(userId), cancellationToken);
        if (status.IsFailure)
        {
            return Failure<AgencyVerificationDocumentContentResponse>(
                status.Error,
                "Verification document download failed.");
        }

        var result = await _sender.Send(
            new GetAgencyVerificationDocumentContentQuery(
                userId,
                status.Value!.AgencyId,
                documentType,
                IsAuthorizedAdmin: false),
            cancellationToken);

        return result.IsFailure
            ? Failure<AgencyVerificationDocumentContentResponse>(
                result.Error,
                "Verification document download failed.")
            : File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetMyAgencyProfileQuery(User.GetRequiredUserId()),
            cancellationToken);

        return result.IsFailure
            ? Failure<AgencyProfileResponse>(result.Error, "Failed to load agency profile.")
            : Ok(ApiResponse<AgencyProfileResponse>.SuccessResponse(
                result.Value!,
                "Agency profile loaded.",
                StatusCodes.Status200OK));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateAgencyProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateMyAgencyProfileCommand(
                User.GetRequiredUserId(),
                request.CompanyName,
                request.Description,
                request.Address,
                request.City,
                request.Country,
                request.CompanyPhone,
                request.LogoUrl,
                request.WebsiteUrl),
            cancellationToken);

        return result.IsFailure
            ? Failure<AgencyProfileResponse>(result.Error, "Agency profile update failed.")
            : Ok(ApiResponse<AgencyProfileResponse>.SuccessResponse(
                result.Value!,
                "Agency profile updated successfully.",
                StatusCodes.Status200OK));
    }

    [HttpGet("dashboard/summary")]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetAgencyDashboardSummaryQuery(User.GetRequiredUserId()),
            cancellationToken);

        return result.IsFailure
            ? Failure<AgencyDashboardSummaryResponse>(result.Error, "Failed to load dashboard summary.")
            : Ok(ApiResponse<AgencyDashboardSummaryResponse>.SuccessResponse(
                result.Value!,
                "Dashboard summary loaded.",
                StatusCodes.Status200OK));
    }

    [HttpGet("dashboard/trends")]
    public async Task<IActionResult> GetDashboardTrends(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetAgencyDashboardTrendsQuery(User.GetRequiredUserId(), days),
            cancellationToken);

        return result.IsFailure
            ? Failure<IReadOnlyList<AgencyDashboardTrendPointResponse>>(result.Error, "Failed to load dashboard trends.")
            : Ok(ApiResponse<IReadOnlyList<AgencyDashboardTrendPointResponse>>.SuccessResponse(
                result.Value!,
                "Dashboard trends loaded.",
                StatusCodes.Status200OK));
    }

    [HttpGet("dashboard/recent-activity")]
    public async Task<IActionResult> GetDashboardRecentActivity(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetAgencyDashboardRecentActivityQuery(User.GetRequiredUserId(), count),
            cancellationToken);

        return result.IsFailure
            ? Failure<IReadOnlyList<AgencyActivityItemResponse>>(result.Error, "Failed to load recent activity.")
            : Ok(ApiResponse<IReadOnlyList<AgencyActivityItemResponse>>.SuccessResponse(
                result.Value!,
                "Recent activity loaded.",
                StatusCodes.Status200OK));
    }

    [HttpPut("reviews/{reviewId:guid}/response")]
    public async Task<IActionResult> RespondToReview(
        Guid reviewId,
        [FromBody] RespondToAgencyReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RespondToAgencyReviewCommand(User.GetRequiredUserId(), reviewId, request.Response),
            cancellationToken);
        return result.IsFailure
            ? Failure<PublicAgencyReviewResponse>(result.Error, "Failed to publish the agency response.")
            : Ok(ApiResponse<PublicAgencyReviewResponse>.SuccessResponse(
                result.Value!, "Agency response published.", StatusCodes.Status200OK));
    }

    private ObjectResult Failure<T>(Error error, string message)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return StatusCode(
            statusCode,
            ApiResponse<T>.FailureResponse(
                [new ApiError(error.Code, error.Message)],
                message,
                statusCode));
    }

    public sealed record UploadAgencyVerificationDocumentRequest(
        string DocumentNumber,
        DateTime? ExpiryDate,
        IFormFile File);

    public sealed record UpdateAgencyProfileRequest(
        string CompanyName,
        string Description,
        string Address,
        string City,
        string Country,
        string CompanyPhone,
        string? LogoUrl,
        string? WebsiteUrl);

    public sealed record CompleteAgencyOnboardingRequest(
        string CompanyName,
        string Description,
        string Address,
        string City,
        string Country,
        string CompanyPhone,
        string? LogoUrl,
        string? WebsiteUrl);

    public sealed record RespondToAgencyReviewRequest(string Response);
}
