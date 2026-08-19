using FluentValidation;
using MediatR;
using Safarni.Application.Abstractions.Notifications;
using Safarni.Application.Abstractions.Payments;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Packages;

public sealed record BookingDisputeResponse(
    Guid Id,
    Guid BookingRequestId,
    Guid OpenedByUserId,
    string Reason,
    decimal? RequestedRefundAmount,
    BookingDisputeStatus Status,
    string? AdminNote,
    decimal? ResolvedRefundAmount,
    DateTime CreatedAt,
    DateTime? ResolvedAt);

public sealed record OpenBookingDisputeCommand(
    Guid TravelerUserId,
    Guid RequestId,
    string Reason,
    decimal? RequestedRefundAmount) : IRequest<Result<BookingDisputeResponse>>;

public sealed record OpenAgencyBookingDisputeCommand(
    Guid AgencyUserId,
    Guid RequestId,
    string Reason) : IRequest<Result<BookingDisputeResponse>>;

public sealed record ListBookingDisputesQuery(Guid AdminUserId)
    : IRequest<Result<IReadOnlyList<BookingDisputeResponse>>>;

public sealed record ResolveBookingDisputeCommand(
    Guid AdminUserId,
    Guid DisputeId,
    BookingDisputeStatus Decision,
    decimal? RefundAmount,
    string? AdminNote) : IRequest<Result<BookingDisputeResponse>>;

public sealed class OpenBookingDisputeCommandValidator : AbstractValidator<OpenBookingDisputeCommand>
{
    public OpenBookingDisputeCommandValidator()
    {
        RuleFor(command => command.TravelerUserId).NotEmpty();
        RuleFor(command => command.RequestId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(2000);
        RuleFor(command => command.RequestedRefundAmount).GreaterThanOrEqualTo(0);
    }
}

public sealed class OpenAgencyBookingDisputeCommandValidator : AbstractValidator<OpenAgencyBookingDisputeCommand>
{
    public OpenAgencyBookingDisputeCommandValidator()
    {
        RuleFor(command => command.AgencyUserId).NotEmpty();
        RuleFor(command => command.RequestId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(2000);
    }
}

public sealed class ResolveBookingDisputeCommandValidator : AbstractValidator<ResolveBookingDisputeCommand>
{
    public ResolveBookingDisputeCommandValidator()
    {
        RuleFor(command => command.AdminUserId).NotEmpty();
        RuleFor(command => command.DisputeId).NotEmpty();
        RuleFor(command => command.Decision).Must(status => status is BookingDisputeStatus.ResolvedForTraveler or BookingDisputeStatus.ResolvedForAgency or BookingDisputeStatus.Rejected);
        RuleFor(command => command.RefundAmount).GreaterThanOrEqualTo(0);
        RuleFor(command => command.AdminNote).MaximumLength(2000);
    }
}

internal static class BookingDisputeMapping
{
    public static BookingDisputeResponse ToResponse(BookingDispute dispute) => new(
        dispute.Id, dispute.PackageBookingRequestId, dispute.OpenedByUserId, dispute.Reason,
        dispute.RequestedRefundAmount, dispute.Status, dispute.AdminNote,
        dispute.ResolvedRefundAmount, dispute.CreatedAt, dispute.ResolvedAt);
}

internal static class BookingDisputeRules
{
    public static bool CanOpen(PackageBookingRequest request) =>
        request.Status is PackageBookingRequestStatus.Confirmed or
            PackageBookingRequestStatus.InProgress or
            PackageBookingRequestStatus.Completed;

    public static void HoldPayouts(PackageBookingRequest request)
    {
        foreach (var payout in request.Payouts.Where(payout => payout.Status is BookingPayoutStatus.PendingReview or BookingPayoutStatus.Approved or BookingPayoutStatus.Held))
            payout.Status = BookingPayoutStatus.Held;
    }
}

public sealed class OpenBookingDisputeCommandHandler : IRequestHandler<OpenBookingDisputeCommand, Result<BookingDisputeResponse>>
{
    private readonly IPackageBookingRequestRepository _requests;
    public OpenBookingDisputeCommandHandler(IPackageBookingRequestRepository requests) => _requests = requests;

    public async Task<Result<BookingDisputeResponse>> Handle(OpenBookingDisputeCommand command, CancellationToken cancellationToken)
    {
        var request = await _requests.GetByIdForTravelerAsync(command.RequestId, command.TravelerUserId, cancellationToken);
        if (request is null) return Result<BookingDisputeResponse>.Failure(PackageBookingErrors.NotFound);
        if (!BookingDisputeRules.CanOpen(request) || request.Disputes.Any(dispute => dispute.Status is BookingDisputeStatus.Open or BookingDisputeStatus.UnderReview))
            return Result<BookingDisputeResponse>.Failure(PackageBookingErrors.DisputeNotAllowed);

        var dispute = new BookingDispute
        {
            Id = Guid.NewGuid(), PackageBookingRequestId = request.Id, OpenedByUserId = command.TravelerUserId,
            Reason = command.Reason.Trim(), RequestedRefundAmount = command.RequestedRefundAmount,
            Status = BookingDisputeStatus.Open, CreatedAt = DateTime.UtcNow
        };
        request.Status = PackageBookingRequestStatus.Disputed;
        request.UpdatedAt = DateTime.UtcNow;
        BookingDisputeRules.HoldPayouts(request);
        await _requests.AddDisputeAsync(dispute, cancellationToken);
        await _requests.SaveChangesAsync(cancellationToken);
        return Result<BookingDisputeResponse>.Success(BookingDisputeMapping.ToResponse(dispute));
    }
}

public sealed class OpenAgencyBookingDisputeCommandHandler : IRequestHandler<OpenAgencyBookingDisputeCommand, Result<BookingDisputeResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly IPackageBookingRequestRepository _requests;
    public OpenAgencyBookingDisputeCommandHandler(ITravelAgencyRepository agencies, IPackageBookingRequestRepository requests)
    { _agencies = agencies; _requests = requests; }

    public async Task<Result<BookingDisputeResponse>> Handle(OpenAgencyBookingDisputeCommand command, CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(command.AgencyUserId, cancellationToken: cancellationToken);
        if (agency is null) return Result<BookingDisputeResponse>.Failure(TravelAgencyErrors.AgencyAccountRequired);
        var request = await _requests.GetByIdForAgencyAsync(command.RequestId, agency.Id, cancellationToken);
        if (request is null) return Result<BookingDisputeResponse>.Failure(PackageBookingErrors.NotFound);
        if (!BookingDisputeRules.CanOpen(request) || request.Disputes.Any(dispute => dispute.Status is BookingDisputeStatus.Open or BookingDisputeStatus.UnderReview))
            return Result<BookingDisputeResponse>.Failure(PackageBookingErrors.DisputeNotAllowed);

        var dispute = new BookingDispute
        {
            Id = Guid.NewGuid(), PackageBookingRequestId = request.Id, OpenedByUserId = command.AgencyUserId,
            Reason = command.Reason.Trim(), Status = BookingDisputeStatus.Open, CreatedAt = DateTime.UtcNow
        };
        request.Status = PackageBookingRequestStatus.Disputed;
        request.UpdatedAt = DateTime.UtcNow;
        BookingDisputeRules.HoldPayouts(request);
        await _requests.AddDisputeAsync(dispute, cancellationToken);
        await _requests.SaveChangesAsync(cancellationToken);
        return Result<BookingDisputeResponse>.Success(BookingDisputeMapping.ToResponse(dispute));
    }
}

public sealed class ListBookingDisputesQueryHandler : IRequestHandler<ListBookingDisputesQuery, Result<IReadOnlyList<BookingDisputeResponse>>>
{
    private readonly IPackageBookingRequestRepository _requests;
    public ListBookingDisputesQueryHandler(IPackageBookingRequestRepository requests) => _requests = requests;
    public async Task<Result<IReadOnlyList<BookingDisputeResponse>>> Handle(ListBookingDisputesQuery query, CancellationToken cancellationToken) =>
        Result<IReadOnlyList<BookingDisputeResponse>>.Success((await _requests.ListDisputesAsync(cancellationToken)).Select(BookingDisputeMapping.ToResponse).ToList());
}

public sealed class ResolveBookingDisputeCommandHandler : IRequestHandler<ResolveBookingDisputeCommand, Result<BookingDisputeResponse>>
{
    private readonly IPackageBookingRequestRepository _requests;
    private readonly IPaymentGateway _payments;
    private readonly IMarketplaceNotificationService _notifications;

    public ResolveBookingDisputeCommandHandler(IPackageBookingRequestRepository requests, IPaymentGateway payments, IMarketplaceNotificationService notifications)
    { _requests = requests; _payments = payments; _notifications = notifications; }

    public async Task<Result<BookingDisputeResponse>> Handle(ResolveBookingDisputeCommand command, CancellationToken cancellationToken)
    {
        var dispute = await _requests.GetDisputeAsync(command.DisputeId, cancellationToken);
        if (dispute is null) return Result<BookingDisputeResponse>.Failure(PackageBookingErrors.NotFound);
        if (dispute.Status is not (BookingDisputeStatus.Open or BookingDisputeStatus.UnderReview))
            return Result<BookingDisputeResponse>.Failure(PackageBookingErrors.DisputeNotAllowed);

        var request = dispute.PackageBookingRequest;
        var payment = request.Payments.Where(payment => payment.Status == BookingPaymentStatus.Succeeded).OrderByDescending(payment => payment.PaidAt).FirstOrDefault();
        var refundAmount = command.RefundAmount ?? (command.Decision == BookingDisputeStatus.ResolvedForTraveler ? payment?.GrossAmount ?? 0m : 0m);
        if (refundAmount < 0 || (payment is not null && refundAmount > payment.GrossAmount))
            return Result<BookingDisputeResponse>.Failure(PackageBookingErrors.DisputeNotAllowed);

        if (command.Decision == BookingDisputeStatus.ResolvedForTraveler && payment is not null && refundAmount > 0)
        {
            try
            {
                await _payments.RefundPaymentAsync(new PaymentRefundRequest(payment.Provider, payment.PaymentSessionId, refundAmount, payment.CurrencyCode, "Booking dispute resolution"), cancellationToken);
            }
            catch (InvalidOperationException exception) when (exception.Message.Contains("not configured", StringComparison.OrdinalIgnoreCase))
            {
                return Result<BookingDisputeResponse>.Failure(PackageBookingErrors.PaymentProviderNotConfigured);
            }
            payment.Status = BookingPaymentStatus.Refunded;
            payment.RefundedAt = DateTime.UtcNow;
            payment.RefundedAmount = refundAmount;
            payment.RefundPercentage = payment.GrossAmount == 0 ? 100m : decimal.Round(refundAmount / payment.GrossAmount * 100m, 2);
            request.Status = PackageBookingRequestStatus.Refunded;
            foreach (var payout in request.Payouts.Where(payout => payout.Status != BookingPayoutStatus.Paid)) payout.Status = BookingPayoutStatus.Rejected;
        }
        else
        {
            request.Status = request.TravelPackage.EndDate <= DateTime.UtcNow ? PackageBookingRequestStatus.Completed : PackageBookingRequestStatus.Confirmed;
            foreach (var payout in request.Payouts.Where(payout => payout.Status == BookingPayoutStatus.Held))
                payout.Status = command.Decision == BookingDisputeStatus.ResolvedForAgency ? BookingPayoutStatus.Approved : BookingPayoutStatus.Rejected;
        }

        dispute.Status = command.Decision;
        dispute.AdminNote = string.IsNullOrWhiteSpace(command.AdminNote) ? null : command.AdminNote.Trim();
        dispute.ResolvedRefundAmount = refundAmount > 0 ? refundAmount : null;
        dispute.ResolvedByUserId = command.AdminUserId;
        dispute.ResolvedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        await _requests.SaveChangesAsync(cancellationToken);
        await _notifications.NotifyUserAsync(request.TravelerUserId, MarketplaceNotificationType.PackageBookingPaymentSucceeded, "Booking dispute resolved", "The Safarni team has completed the review of your booking dispute.", "/agency-trips", cancellationToken);
        return Result<BookingDisputeResponse>.Success(BookingDisputeMapping.ToResponse(dispute));
    }
}
