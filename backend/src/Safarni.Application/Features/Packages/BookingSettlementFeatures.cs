using FluentValidation;
using MediatR;
using Safarni.Application.Abstractions.Notifications;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Storage;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Packages;

public sealed record SupplierEvidenceResponse(
    Guid Id,
    Guid BookingRequestId,
    Guid OfferId,
    Guid AgencyId,
    string SupplierType,
    string SupplierName,
    string ConfirmationCode,
    decimal InvoiceAmount,
    string CurrencyCode,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    BookingSupplierEvidenceStatus Status,
    string? ReviewNote,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    BookingPayoutStatus? PayoutStatus,
    decimal? PayoutAmount);

public sealed record BookingPayoutResponse(
    Guid Id,
    Guid BookingRequestId,
    Guid AgencyId,
    BookingPayoutType Type,
    BookingPayoutStatus Status,
    decimal Amount,
    string CurrencyCode,
    string? ExternalTransferId,
    DateTime? ReleaseAt,
    DateTime CreatedAt,
    DateTime? PaidAt);

public sealed record SubmitSupplierEvidenceCommand(
    Guid AgencyUserId,
    Guid RequestId,
    string SupplierType,
    string SupplierName,
    string ConfirmationCode,
    decimal InvoiceAmount,
    string CurrencyCode,
    Stream FileStream,
    string OriginalFileName,
    string ContentType,
    long SizeBytes)
    : IRequest<Result<SupplierEvidenceResponse>>;

public sealed record ListPendingSupplierEvidenceQuery(Guid AdminUserId)
    : IRequest<Result<IReadOnlyList<SupplierEvidenceResponse>>>;

public sealed record ReviewSupplierEvidenceCommand(
    Guid AdminUserId,
    Guid EvidenceId,
    bool Approve,
    string? ReviewNote)
    : IRequest<Result<SupplierEvidenceResponse>>;

public sealed record MarkBookingPayoutPaidCommand(
    Guid AdminUserId,
    Guid EvidenceId,
    string ExternalTransferId)
    : IRequest<Result<SupplierEvidenceResponse>>;

public sealed record MarkBookingPayoutPaidByIdCommand(
    Guid AdminUserId,
    Guid PayoutId,
    string ExternalTransferId)
    : IRequest<Result<BookingPayoutResponse>>;

public sealed record ListActionableBookingPayoutsQuery(Guid AdminUserId)
    : IRequest<Result<IReadOnlyList<BookingPayoutResponse>>>;

public sealed class SubmitSupplierEvidenceCommandValidator : AbstractValidator<SubmitSupplierEvidenceCommand>
{
    public SubmitSupplierEvidenceCommandValidator()
    {
        RuleFor(command => command.AgencyUserId).NotEmpty();
        RuleFor(command => command.RequestId).NotEmpty();
        RuleFor(command => command.SupplierType).NotEmpty().MaximumLength(50);
        RuleFor(command => command.SupplierName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.ConfirmationCode).NotEmpty().MaximumLength(200);
        RuleFor(command => command.InvoiceAmount).GreaterThan(0);
        RuleFor(command => command.CurrencyCode).NotEmpty().Matches("^[A-Za-z]{3}$");
    }
}

public sealed class ReviewSupplierEvidenceCommandValidator : AbstractValidator<ReviewSupplierEvidenceCommand>
{
    public ReviewSupplierEvidenceCommandValidator()
    {
        RuleFor(command => command.AdminUserId).NotEmpty();
        RuleFor(command => command.EvidenceId).NotEmpty();
        RuleFor(command => command.ReviewNote).MaximumLength(1000);
    }
}

public sealed class MarkBookingPayoutPaidCommandValidator : AbstractValidator<MarkBookingPayoutPaidCommand>
{
    public MarkBookingPayoutPaidCommandValidator()
    {
        RuleFor(command => command.AdminUserId).NotEmpty();
        RuleFor(command => command.EvidenceId).NotEmpty();
        RuleFor(command => command.ExternalTransferId).NotEmpty().MaximumLength(200);
    }
}

public sealed class MarkBookingPayoutPaidByIdCommandValidator : AbstractValidator<MarkBookingPayoutPaidByIdCommand>
{
    public MarkBookingPayoutPaidByIdCommandValidator()
    {
        RuleFor(command => command.AdminUserId).NotEmpty();
        RuleFor(command => command.PayoutId).NotEmpty();
        RuleFor(command => command.ExternalTransferId).NotEmpty().MaximumLength(200);
    }
}

internal static class SupplierEvidenceMapping
{
    public static SupplierEvidenceResponse ToResponse(BookingSupplierEvidence evidence, BookingPayout? payout) => new(
        evidence.Id, evidence.PackageBookingRequestId, evidence.PackageBookingOfferId, evidence.TravelAgencyId,
        evidence.SupplierType, evidence.SupplierName, evidence.ConfirmationCode, evidence.InvoiceAmount,
        evidence.CurrencyCode, evidence.OriginalFileName, evidence.ContentType, evidence.SizeBytes,
        evidence.Status, evidence.ReviewNote, evidence.CreatedAt, evidence.ReviewedAt,
        payout?.Status, payout?.Amount);

    public static BookingPayoutResponse ToPayoutResponse(BookingPayout payout) => new(
        payout.Id, payout.PackageBookingRequestId, payout.TravelAgencyId, payout.Type,
        payout.Status, payout.Amount, payout.CurrencyCode, payout.ExternalTransferId,
        payout.ReleaseAt, payout.CreatedAt, payout.PaidAt);
}

public sealed class SubmitSupplierEvidenceCommandHandler
    : IRequestHandler<SubmitSupplierEvidenceCommand, Result<SupplierEvidenceResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly IPackageBookingRequestRepository _requests;
    private readonly IBookingDocumentStorage _storage;

    public SubmitSupplierEvidenceCommandHandler(ITravelAgencyRepository agencies, IPackageBookingRequestRepository requests, IBookingDocumentStorage storage)
    { _agencies = agencies; _requests = requests; _storage = storage; }

    public async Task<Result<SupplierEvidenceResponse>> Handle(SubmitSupplierEvidenceCommand command, CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(command.AgencyUserId, cancellationToken: cancellationToken);
        if (agency is null) return Result<SupplierEvidenceResponse>.Failure(TravelAgencyErrors.AgencyAccountRequired);
        var request = await _requests.GetByIdForAgencyAsync(command.RequestId, agency.Id, cancellationToken);
        if (request is null) return Result<SupplierEvidenceResponse>.Failure(PackageBookingErrors.NotFound);
        if (request.Status != PackageBookingRequestStatus.Confirmed)
            return Result<SupplierEvidenceResponse>.Failure(PackageBookingErrors.SupplierEvidenceNotAllowed);
        var offer = request.Offers.FirstOrDefault(item => item.Status == PackageBookingOfferStatus.Accepted);
        var paid = request.Payments.Any(payment => payment.Status == BookingPaymentStatus.Succeeded);
        if (offer is null || !paid) return Result<SupplierEvidenceResponse>.Failure(PackageBookingErrors.SupplierEvidenceNotAllowed);
        if (command.SizeBytes <= 0 || command.SizeBytes > 10 * 1024 * 1024)
            return Result<SupplierEvidenceResponse>.Failure(PackageBookingErrors.SupplierEvidenceNotAllowed);
        var extension = Path.GetExtension(command.OriginalFileName);
        if (!new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return Result<SupplierEvidenceResponse>.Failure(PackageBookingErrors.SupplierEvidenceNotAllowed);

        var reference = await _storage.SaveAsync(request.Id, command.FileStream, extension, cancellationToken);
        var evidence = new BookingSupplierEvidence
        {
            Id = Guid.NewGuid(), PackageBookingRequestId = request.Id, PackageBookingOfferId = offer.Id,
            TravelAgencyId = agency.Id, UploadedByUserId = command.AgencyUserId,
            SupplierType = command.SupplierType.Trim(), SupplierName = command.SupplierName.Trim(),
            ConfirmationCode = command.ConfirmationCode.Trim(), InvoiceAmount = command.InvoiceAmount,
            CurrencyCode = command.CurrencyCode.Trim().ToUpperInvariant(), StorageReference = reference,
            OriginalFileName = Path.GetFileName(command.OriginalFileName), ContentType = command.ContentType,
            SizeBytes = command.SizeBytes, CreatedAt = DateTime.UtcNow
        };
        var supplierCap = offer.SupplierCostAmount > 0 ? offer.SupplierCostAmount : offer.Amount;
        var payout = new BookingPayout
        {
            Id = Guid.NewGuid(), PackageBookingRequestId = request.Id, PackageBookingOfferId = offer.Id,
            TravelAgencyId = agency.Id, SupplierEvidenceId = evidence.Id, Type = BookingPayoutType.SupplierCostAdvance,
            Amount = Math.Min(command.InvoiceAmount, supplierCap), CurrencyCode = evidence.CurrencyCode,
            Status = BookingPayoutStatus.PendingReview, CreatedAt = DateTime.UtcNow
        };
        await _requests.AddEvidenceAsync(evidence, cancellationToken);
        await _requests.AddPayoutAsync(payout, cancellationToken);
        await _requests.SaveChangesAsync(cancellationToken);
        return Result<SupplierEvidenceResponse>.Success(SupplierEvidenceMapping.ToResponse(evidence, payout));
    }
}

public sealed class ListPendingSupplierEvidenceQueryHandler
    : IRequestHandler<ListPendingSupplierEvidenceQuery, Result<IReadOnlyList<SupplierEvidenceResponse>>>
{
    private readonly IPackageBookingRequestRepository _requests;
    public ListPendingSupplierEvidenceQueryHandler(IPackageBookingRequestRepository requests) => _requests = requests;

    public async Task<Result<IReadOnlyList<SupplierEvidenceResponse>>> Handle(ListPendingSupplierEvidenceQuery query, CancellationToken cancellationToken)
    {
        var evidence = await _requests.ListPendingEvidenceAsync(cancellationToken);
        var responses = new List<SupplierEvidenceResponse>();
        foreach (var item in evidence)
            responses.Add(SupplierEvidenceMapping.ToResponse(item, await _requests.GetPayoutByEvidenceAsync(item.Id, cancellationToken)));
        return Result<IReadOnlyList<SupplierEvidenceResponse>>.Success(responses);
    }
}

public sealed class ReviewSupplierEvidenceCommandHandler
    : IRequestHandler<ReviewSupplierEvidenceCommand, Result<SupplierEvidenceResponse>>
{
    private readonly IPackageBookingRequestRepository _requests;
    private readonly ITravelAgencyRepository _agencies;

    public ReviewSupplierEvidenceCommandHandler(IPackageBookingRequestRepository requests, ITravelAgencyRepository agencies)
    { _requests = requests; _agencies = agencies; }

    public async Task<Result<SupplierEvidenceResponse>> Handle(ReviewSupplierEvidenceCommand command, CancellationToken cancellationToken)
    {
        var evidence = await _requests.GetEvidenceAsync(command.EvidenceId, cancellationToken);
        if (evidence is null) return Result<SupplierEvidenceResponse>.Failure(PackageBookingErrors.NotFound);
        if (evidence.Status != BookingSupplierEvidenceStatus.PendingReview)
            return Result<SupplierEvidenceResponse>.Failure(PackageBookingErrors.PayoutNotAllowed);
        var payoutProfile = await _agencies.GetByIdAsync(evidence.TravelAgencyId, cancellationToken: cancellationToken);
        var payout = await _requests.GetPayoutByEvidenceAsync(evidence.Id, cancellationToken);
        if (payout is null) return Result<SupplierEvidenceResponse>.Failure(PackageBookingErrors.PayoutNotAllowed);
        evidence.Status = command.Approve ? BookingSupplierEvidenceStatus.Approved : BookingSupplierEvidenceStatus.Rejected;
        evidence.ReviewedByUserId = command.AdminUserId;
        evidence.ReviewedAt = DateTime.UtcNow;
        evidence.ReviewNote = string.IsNullOrWhiteSpace(command.ReviewNote) ? null : command.ReviewNote.Trim();
        payout.Status = command.Approve && payoutProfile?.PayoutProfile?.Status == AgencyPayoutProfileStatus.Verified
            ? BookingPayoutStatus.Approved
            : command.Approve ? BookingPayoutStatus.Held : BookingPayoutStatus.Rejected;
        payout.ReviewedAt = DateTime.UtcNow;
        payout.ReviewNote = payout.Status == BookingPayoutStatus.Held ? "Awaiting verified agency payout details." : evidence.ReviewNote;
        await _requests.SaveChangesAsync(cancellationToken);
        return Result<SupplierEvidenceResponse>.Success(SupplierEvidenceMapping.ToResponse(evidence, payout));
    }
}

public sealed class MarkBookingPayoutPaidCommandHandler
    : IRequestHandler<MarkBookingPayoutPaidCommand, Result<SupplierEvidenceResponse>>
{
    private readonly IPackageBookingRequestRepository _requests;
    private readonly IMarketplaceNotificationService _notifications;

    public MarkBookingPayoutPaidCommandHandler(IPackageBookingRequestRepository requests, IMarketplaceNotificationService notifications)
    { _requests = requests; _notifications = notifications; }

    public async Task<Result<SupplierEvidenceResponse>> Handle(MarkBookingPayoutPaidCommand command, CancellationToken cancellationToken)
    {
        var evidence = await _requests.GetEvidenceAsync(command.EvidenceId, cancellationToken);
        if (evidence is null) return Result<SupplierEvidenceResponse>.Failure(PackageBookingErrors.NotFound);
        var payout = await _requests.GetPayoutByEvidenceAsync(command.EvidenceId, cancellationToken);
        if (payout is null || payout.Status != BookingPayoutStatus.Approved)
            return Result<SupplierEvidenceResponse>.Failure(PackageBookingErrors.PayoutNotAllowed);
        payout.Status = BookingPayoutStatus.Paid;
        payout.ExternalTransferId = command.ExternalTransferId.Trim();
        payout.PaidAt = DateTime.UtcNow;
        await _requests.SaveChangesAsync(cancellationToken);
        await _notifications.NotifyUserAsync(
            evidence.TravelAgency.UserId,
            MarketplaceNotificationType.PackageBookingPaymentSucceeded,
            "Supplier payout released",
            $"A supplier advance of {payout.Amount:0.00} {payout.CurrencyCode} was marked as paid for {evidence.PackageBookingRequest.TravelPackage.Title}.",
            "/agency/booking-requests",
            cancellationToken);
        return Result<SupplierEvidenceResponse>.Success(SupplierEvidenceMapping.ToResponse(evidence, payout));
    }
}

public sealed class MarkBookingPayoutPaidByIdCommandHandler
    : IRequestHandler<MarkBookingPayoutPaidByIdCommand, Result<BookingPayoutResponse>>
{
    private readonly IPackageBookingRequestRepository _requests;
    private readonly IMarketplaceNotificationService _notifications;

    public MarkBookingPayoutPaidByIdCommandHandler(IPackageBookingRequestRepository requests, IMarketplaceNotificationService notifications)
    { _requests = requests; _notifications = notifications; }

    public async Task<Result<BookingPayoutResponse>> Handle(MarkBookingPayoutPaidByIdCommand command, CancellationToken cancellationToken)
    {
        var payout = await _requests.GetPayoutAsync(command.PayoutId, cancellationToken);
        if (payout is null) return Result<BookingPayoutResponse>.Failure(PackageBookingErrors.NotFound);
        var isHeldAndReady = payout.Status == BookingPayoutStatus.Held && payout.ReleaseAt <= DateTime.UtcNow;
        if (payout.Status != BookingPayoutStatus.Approved && !isHeldAndReady)
            return Result<BookingPayoutResponse>.Failure(PackageBookingErrors.PayoutNotAllowed);
        payout.Status = BookingPayoutStatus.Paid;
        payout.ExternalTransferId = command.ExternalTransferId.Trim();
        payout.PaidAt = DateTime.UtcNow;
        await _requests.SaveChangesAsync(cancellationToken);
        await _notifications.NotifyUserAsync(
            payout.TravelAgency.UserId,
            MarketplaceNotificationType.PackageBookingPaymentSucceeded,
            "Agency payout released",
            $"A payout of {payout.Amount:0.00} {payout.CurrencyCode} was marked as paid for {payout.PackageBookingRequest.TravelPackage.Title}.",
            "/agency/booking-requests",
            cancellationToken);
        return Result<BookingPayoutResponse>.Success(SupplierEvidenceMapping.ToPayoutResponse(payout));
    }
}

public sealed class ListActionableBookingPayoutsQueryHandler
    : IRequestHandler<ListActionableBookingPayoutsQuery, Result<IReadOnlyList<BookingPayoutResponse>>>
{
    private readonly IPackageBookingRequestRepository _requests;
    public ListActionableBookingPayoutsQueryHandler(IPackageBookingRequestRepository requests) => _requests = requests;
    public async Task<Result<IReadOnlyList<BookingPayoutResponse>>> Handle(ListActionableBookingPayoutsQuery query, CancellationToken cancellationToken) =>
        Result<IReadOnlyList<BookingPayoutResponse>>.Success((await _requests.ListActionablePayoutsAsync(cancellationToken)).Select(SupplierEvidenceMapping.ToPayoutResponse).ToList());
}
