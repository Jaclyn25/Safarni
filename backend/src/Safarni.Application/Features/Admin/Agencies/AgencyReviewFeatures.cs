using FluentValidation;
using MediatR;
using Safarni.Application.Abstractions.Auditing;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Notifications;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Agencies.Common;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Admin.Agencies;

public sealed record AdminAgencySummaryResponse(
    Guid AgencyId,
    string CompanyName,
    string RepresentativeName,
    string Email,
    string City,
    string Country,
    AgencyVerificationStatus VerificationStatus,
    int UploadedDocumentCount,
    int RequiredDocumentCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record AdminAgencyDetailResponse(
    AgencyProfileResponse Profile,
    AgencyVerificationStatusResponse Verification,
    Guid? ReviewedByUserId);

public sealed record ListAdminAgenciesQuery(
    AgencyVerificationStatus? Status,
    string? Search,
    int Page,
    int PageSize)
    : IRequest<Result<PagedResult<AdminAgencySummaryResponse>>>;

public sealed class ListAdminAgenciesQueryHandler
    : IRequestHandler<ListAdminAgenciesQuery, Result<PagedResult<AdminAgencySummaryResponse>>>
{
    private readonly ITravelAgencyRepository _agencies;

    public ListAdminAgenciesQueryHandler(ITravelAgencyRepository agencies)
    {
        _agencies = agencies;
    }

    public async Task<Result<PagedResult<AdminAgencySummaryResponse>>> Handle(
        ListAdminAgenciesQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var result = await _agencies.ListAsync(
            request.Status,
            request.Search,
            page,
            pageSize,
            cancellationToken);

        var items = result.Items.Select(ToSummary).ToArray();
        return Result<PagedResult<AdminAgencySummaryResponse>>.Success(
            new PagedResult<AdminAgencySummaryResponse>(
                items,
                result.Page,
                result.PageSize,
                result.TotalCount));
    }

    private static AdminAgencySummaryResponse ToSummary(TravelAgency agency)
    {
        return new AdminAgencySummaryResponse(
            agency.Id,
            agency.CompanyName,
            agency.User.FullName,
            agency.User.Email,
            agency.City,
            agency.Country,
            agency.VerificationStatus,
            agency.VerificationDocuments.Count,
            Enum.GetValues<AgencyVerificationDocumentType>().Length,
            agency.CreatedAt,
            agency.UpdatedAt);
    }
}

public sealed record GetAdminAgencyQuery(Guid AgencyId)
    : IRequest<Result<AdminAgencyDetailResponse>>;

public sealed class GetAdminAgencyQueryHandler
    : IRequestHandler<GetAdminAgencyQuery, Result<AdminAgencyDetailResponse>>
{
    private readonly ITravelAgencyRepository _agencies;

    public GetAdminAgencyQueryHandler(ITravelAgencyRepository agencies)
    {
        _agencies = agencies;
    }

    public async Task<Result<AdminAgencyDetailResponse>> Handle(
        GetAdminAgencyQuery request,
        CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByIdAsync(
            request.AgencyId,
            includeDocuments: true,
            cancellationToken);
        if (agency is null)
        {
            return Result<AdminAgencyDetailResponse>.Failure(TravelAgencyErrors.NotFound);
        }

        return Result<AdminAgencyDetailResponse>.Success(
            new AdminAgencyDetailResponse(
                AgencyResponseMapper.ToProfile(agency),
                AgencyResponseMapper.ToVerificationStatus(agency),
                agency.VerificationReviewedByUserId));
    }
}

public sealed record DecideAgencyVerificationCommand(
    Guid AgencyId,
    Guid AdminUserId,
    AgencyVerificationDecision Decision,
    string? Reason)
    : IRequest<Result<AdminAgencyDetailResponse>>;

public sealed class DecideAgencyVerificationCommandValidator
    : AbstractValidator<DecideAgencyVerificationCommand>
{
    public DecideAgencyVerificationCommandValidator()
    {
        RuleFor(command => command.AgencyId).NotEmpty();
        RuleFor(command => command.AdminUserId).NotEmpty();
        RuleFor(command => command.Decision).IsInEnum();
        RuleFor(command => command.Reason).MaximumLength(1000);
        RuleFor(command => command.Reason)
            .NotEmpty()
            .When(command => command.Decision != AgencyVerificationDecision.Approve)
            .WithMessage("A reason is required when requesting changes, rejecting, or suspending an agency.");
    }
}

public sealed class DecideAgencyVerificationCommandHandler
    : IRequestHandler<DecideAgencyVerificationCommand, Result<AdminAgencyDetailResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly IAuditLogWriter _auditLog;
    private readonly IMarketplaceNotificationService _notifications;

    public DecideAgencyVerificationCommandHandler(
        ITravelAgencyRepository agencies,
        IAuditLogWriter auditLog,
        IMarketplaceNotificationService notifications)
    {
        _agencies = agencies;
        _auditLog = auditLog;
        _notifications = notifications;
    }

    public async Task<Result<AdminAgencyDetailResponse>> Handle(
        DecideAgencyVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByIdAsync(
            request.AgencyId,
            includeDocuments: true,
            cancellationToken);
        if (agency is null)
        {
            return Result<AdminAgencyDetailResponse>.Failure(TravelAgencyErrors.NotFound);
        }

        var targetStatus = MapDecision(request.Decision);
        if (!IsAllowedTransition(agency.VerificationStatus, targetStatus))
        {
            return Result<AdminAgencyDetailResponse>.Failure(
                TravelAgencyErrors.InvalidVerificationTransition);
        }

        if (targetStatus == AgencyVerificationStatus.Verified &&
            !HasCompleteValidDocuments(agency.VerificationDocuments))
        {
            return Result<AdminAgencyDetailResponse>.Failure(
                TravelAgencyErrors.RequiredDocumentsMissing);
        }

        var previousStatus = agency.VerificationStatus;
        agency.VerificationStatus = targetStatus;
        agency.VerificationDecisionReason = request.Decision == AgencyVerificationDecision.Approve
            ? null
            : TrimToNull(request.Reason);
        agency.VerificationReviewedAt = DateTime.UtcNow;
        agency.VerificationReviewedByUserId = request.AdminUserId;
        agency.UpdatedAt = agency.VerificationReviewedAt;
        await _agencies.SaveChangesAsync(cancellationToken);

        await _auditLog.WriteAsync(
            request.AdminUserId,
            $"TravelAgency.Verification.{request.Decision}",
            "TravelAgency",
            agency.Id.ToString(),
            new
            {
                PreviousStatus = previousStatus.ToString(),
                NewStatus = targetStatus.ToString(),
                Reason = agency.VerificationDecisionReason
            },
            targetStatus is AgencyVerificationStatus.Rejected or AgencyVerificationStatus.Suspended
                ? AuditSeverity.Warning
                : AuditSeverity.Info,
            cancellationToken);

        await _notifications.NotifyUserAsync(
            agency.UserId,
            MapNotificationType(request.Decision),
            DecisionNotificationTitle(request.Decision),
            DecisionNotificationMessage(request.Decision, agency.VerificationDecisionReason),
            "/agency",
            cancellationToken);

        return Result<AdminAgencyDetailResponse>.Success(
            new AdminAgencyDetailResponse(
                AgencyResponseMapper.ToProfile(agency),
                AgencyResponseMapper.ToVerificationStatus(agency),
                agency.VerificationReviewedByUserId));
    }

    private static bool HasCompleteValidDocuments(
        IEnumerable<AgencyVerificationDocument> documents)
    {
        var today = DateTime.UtcNow.Date;
        var byType = documents.ToDictionary(document => document.DocumentType);
        return Enum.GetValues<AgencyVerificationDocumentType>().All(type =>
            byType.TryGetValue(type, out var document) &&
            document.ReviewStatus == AgencyDocumentReviewStatus.Approved &&
            (document.ExpiryDate is null || document.ExpiryDate.Value.Date >= today));
    }

    private static bool IsAllowedTransition(
        AgencyVerificationStatus current,
        AgencyVerificationStatus target)
    {
        if (current == target)
        {
            return false;
        }

        if (target == AgencyVerificationStatus.Suspended)
        {
            return current == AgencyVerificationStatus.Verified;
        }

        if (current == AgencyVerificationStatus.Suspended)
        {
            return target is AgencyVerificationStatus.Verified or
                AgencyVerificationStatus.ChangesRequested;
        }

        return true;
    }

    private static AgencyVerificationStatus MapDecision(AgencyVerificationDecision decision) =>
        decision switch
        {
            AgencyVerificationDecision.Approve => AgencyVerificationStatus.Verified,
            AgencyVerificationDecision.RequestChanges => AgencyVerificationStatus.ChangesRequested,
            AgencyVerificationDecision.Reject => AgencyVerificationStatus.Rejected,
            AgencyVerificationDecision.Suspend => AgencyVerificationStatus.Suspended,
            _ => throw new ArgumentOutOfRangeException(nameof(decision), decision, null)
        };

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static MarketplaceNotificationType MapNotificationType(
        AgencyVerificationDecision decision) => decision switch
        {
            AgencyVerificationDecision.Approve => MarketplaceNotificationType.AgencyApproved,
            AgencyVerificationDecision.RequestChanges => MarketplaceNotificationType.AgencyChangesRequested,
            AgencyVerificationDecision.Reject => MarketplaceNotificationType.AgencyRejected,
            AgencyVerificationDecision.Suspend => MarketplaceNotificationType.AgencySuspended,
            _ => throw new ArgumentOutOfRangeException(nameof(decision), decision, null)
        };

    private static string DecisionNotificationTitle(AgencyVerificationDecision decision) =>
        decision switch
        {
            AgencyVerificationDecision.Approve => "Agency verification approved",
            AgencyVerificationDecision.RequestChanges => "Agency changes requested",
            AgencyVerificationDecision.Reject => "Agency verification declined",
            AgencyVerificationDecision.Suspend => "Agency account suspended",
            _ => "Agency verification updated"
        };

    private static string DecisionNotificationMessage(
        AgencyVerificationDecision decision,
        string? reason) => decision == AgencyVerificationDecision.Approve
            ? "Your agency is verified. Verified-only marketplace features are now available."
            : string.IsNullOrWhiteSpace(reason)
                ? "An administrator updated your agency verification status."
                : reason;
}

public sealed record DecideAgencyDocumentCommand(
    Guid AgencyId,
    Guid AdminUserId,
    AgencyVerificationDocumentType DocumentType,
    AgencyDocumentDecision Decision,
    string? Reason)
    : IRequest<Result<AdminAgencyDetailResponse>>;

public sealed class DecideAgencyDocumentCommandValidator
    : AbstractValidator<DecideAgencyDocumentCommand>
{
    public DecideAgencyDocumentCommandValidator()
    {
        RuleFor(command => command.AgencyId).NotEmpty();
        RuleFor(command => command.AdminUserId).NotEmpty();
        RuleFor(command => command.DocumentType).IsInEnum();
        RuleFor(command => command.Decision).IsInEnum();
        RuleFor(command => command.Reason).MaximumLength(1000);
        RuleFor(command => command.Reason)
            .NotEmpty()
            .When(command => command.Decision == AgencyDocumentDecision.Decline)
            .WithMessage("Explain what must be corrected when declining a document.");
    }
}

public sealed class DecideAgencyDocumentCommandHandler
    : IRequestHandler<DecideAgencyDocumentCommand, Result<AdminAgencyDetailResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly IAuditLogWriter _auditLog;
    private readonly IMarketplaceNotificationService _notifications;

    public DecideAgencyDocumentCommandHandler(
        ITravelAgencyRepository agencies,
        IAuditLogWriter auditLog,
        IMarketplaceNotificationService notifications)
    {
        _agencies = agencies;
        _auditLog = auditLog;
        _notifications = notifications;
    }

    public async Task<Result<AdminAgencyDetailResponse>> Handle(
        DecideAgencyDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByIdAsync(
            request.AgencyId,
            includeDocuments: true,
            cancellationToken);
        if (agency is null)
        {
            return Result<AdminAgencyDetailResponse>.Failure(TravelAgencyErrors.NotFound);
        }

        var document = agency.VerificationDocuments.SingleOrDefault(
            item => item.DocumentType == request.DocumentType);
        if (document is null)
        {
            return Result<AdminAgencyDetailResponse>.Failure(
                TravelAgencyErrors.DocumentNotFound);
        }

        if (request.Decision == AgencyDocumentDecision.Approve &&
            document.ExpiryDate is not null &&
            document.ExpiryDate.Value.Date < DateTime.UtcNow.Date)
        {
            return Result<AdminAgencyDetailResponse>.Failure(
                TravelAgencyErrors.InvalidDocument);
        }

        var now = DateTime.UtcNow;
        var previousStatus = document.ReviewStatus;
        document.ReviewStatus = request.Decision == AgencyDocumentDecision.Approve
            ? AgencyDocumentReviewStatus.Approved
            : AgencyDocumentReviewStatus.Declined;
        document.ReviewReason = request.Decision == AgencyDocumentDecision.Approve
            ? null
            : request.Reason?.Trim();
        document.ReviewedAt = now;
        document.ReviewedByUserId = request.AdminUserId;
        document.UpdatedAt = now;

        if (request.Decision == AgencyDocumentDecision.Decline)
        {
            agency.VerificationStatus = AgencyVerificationStatus.ChangesRequested;
            agency.VerificationDecisionReason =
                "One or more verification documents require correction.";
            agency.VerificationReviewedAt = now;
            agency.VerificationReviewedByUserId = request.AdminUserId;
        }

        agency.UpdatedAt = now;
        await _agencies.SaveChangesAsync(cancellationToken);

        await _auditLog.WriteAsync(
            request.AdminUserId,
            $"TravelAgency.Document.{request.Decision}",
            "AgencyVerificationDocument",
            document.Id.ToString(),
            new
            {
                AgencyId = agency.Id,
                DocumentType = request.DocumentType.ToString(),
                PreviousStatus = previousStatus.ToString(),
                NewStatus = document.ReviewStatus.ToString(),
                Reason = document.ReviewReason
            },
            request.Decision == AgencyDocumentDecision.Decline
                ? AuditSeverity.Warning
                : AuditSeverity.Info,
            cancellationToken);

        var displayName = AgencyResponseMapper.DisplayName(request.DocumentType);
        await _notifications.NotifyUserAsync(
            agency.UserId,
            request.Decision == AgencyDocumentDecision.Approve
                ? MarketplaceNotificationType.AgencyDocumentApproved
                : MarketplaceNotificationType.AgencyDocumentDeclined,
            request.Decision == AgencyDocumentDecision.Approve
                ? $"{displayName} approved"
                : $"{displayName} needs changes",
            request.Decision == AgencyDocumentDecision.Approve
                ? $"Safarni approved your {displayName}."
                : document.ReviewReason!,
            "/agency/verification",
            cancellationToken);

        return Result<AdminAgencyDetailResponse>.Success(
            new AdminAgencyDetailResponse(
                AgencyResponseMapper.ToProfile(agency),
                AgencyResponseMapper.ToVerificationStatus(agency),
                agency.VerificationReviewedByUserId));
    }
}
