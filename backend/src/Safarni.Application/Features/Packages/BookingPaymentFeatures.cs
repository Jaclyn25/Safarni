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

public sealed record BookingPaymentResponse(
    Guid Id,
    Guid BookingRequestId,
    Guid OfferId,
    string Provider,
    string PaymentSessionId,
    string? CheckoutUrl,
    decimal GrossAmount,
    decimal CommissionRate,
    decimal CommissionAmount,
    decimal AgencyAmount,
    string CurrencyCode,
    BookingPaymentStatus Status,
    DateTime CreatedAt,
    DateTime? PaidAt,
    DateTime? RefundedAt,
    int InstallmentNumber,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    DateTime? NextPaymentDueAt,
    bool IsFullyPaid,
    string? ProviderRefundId = null,
    decimal? RefundedAmount = null,
    decimal? RefundPercentage = null);

public sealed record CreateBookingPaymentCommand(Guid TravelerUserId, Guid RequestId)
    : IRequest<Result<BookingPaymentResponse>>;

public sealed record ProcessBookingPaymentWebhookCommand(PaymentWebhookEvent Event)
    : IRequest<Result<BookingPaymentResponse>>;

public sealed class CreateBookingPaymentCommandValidator
    : AbstractValidator<CreateBookingPaymentCommand>
{
    public CreateBookingPaymentCommandValidator()
    {
        RuleFor(command => command.TravelerUserId).NotEmpty();
        RuleFor(command => command.RequestId).NotEmpty();
    }
}

internal static class BookingPaymentMapping
{
    public static BookingPaymentResponse ToResponse(BookingPayment payment, string? checkoutUrl = null)
    {
        var offer = payment.PackageBookingOffer;
        var totalAmount = offer?.Amount ?? payment.GrossAmount;
        var paidAmount = payment.PackageBookingRequest?.Payments?
            .Where(item => item.Status == BookingPaymentStatus.Succeeded)
            .Sum(item => item.GrossAmount) ?? (payment.Status == BookingPaymentStatus.Succeeded ? payment.GrossAmount : 0m);
        var remainingAmount = Math.Max(0m, totalAmount - paidAmount);

        return new BookingPaymentResponse(
            payment.Id,
            payment.PackageBookingRequestId,
            payment.PackageBookingOfferId,
            payment.Provider,
            payment.PaymentSessionId,
            checkoutUrl,
            payment.GrossAmount,
            payment.CommissionRate,
            payment.CommissionAmount,
            payment.AgencyAmount,
            payment.CurrencyCode,
            payment.Status,
            payment.CreatedAt,
            payment.PaidAt,
            payment.RefundedAt,
            payment.InstallmentNumber,
            totalAmount,
            paidAmount,
            remainingAmount,
            remainingAmount > 0m ? offer?.FinalPaymentDueAt : null,
            remainingAmount <= 0m,
            payment.ProviderRefundId,
            payment.RefundedAmount,
            payment.RefundPercentage);
    }
}

internal static class BookingCancellationPolicy
{
    public static decimal GetRefundPercentage(string? policy, DateTime tripStartUtc, DateTime nowUtc)
    {
        if (nowUtc >= tripStartUtc) return 0m;
        if (string.IsNullOrWhiteSpace(policy)) return 100m;

        var normalized = policy.Trim().ToLowerInvariant();
        if (normalized.Contains("non-refundable") || normalized.Contains("non refundable") || normalized.Contains("غير مسترد"))
            return 0m;

        var rules = System.Text.RegularExpressions.Regex.Matches(
            normalized,
            @"(?<percent>\d{1,3})\s*%[^\d]{0,40}(?<days>\d{1,3})\s*(?:day|days|يوم)");

        decimal? matched = null;
        var daysBeforeTrip = (tripStartUtc - nowUtc).TotalDays;
        foreach (System.Text.RegularExpressions.Match rule in rules)
        {
            if (!decimal.TryParse(rule.Groups["percent"].Value, out var percent) ||
                !int.TryParse(rule.Groups["days"].Value, out var days)) continue;
            if (daysBeforeTrip >= days) matched = matched is null ? percent : Math.Max(matched.Value, percent);
        }

        return Math.Clamp(matched ?? 100m, 0m, 100m);
    }
}

public sealed record RequestBookingRefundCommand(Guid TravelerUserId, Guid RequestId, string Reason)
    : IRequest<Result<BookingPaymentResponse>>;

public sealed class RequestBookingRefundCommandValidator
    : AbstractValidator<RequestBookingRefundCommand>
{
    public RequestBookingRefundCommandValidator()
    {
        RuleFor(command => command.TravelerUserId).NotEmpty();
        RuleFor(command => command.RequestId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class RequestBookingRefundCommandHandler
    : IRequestHandler<RequestBookingRefundCommand, Result<BookingPaymentResponse>>
{
    private readonly IPackageBookingRequestRepository _requests;
    private readonly IPaymentGateway _gateway;
    private readonly IMarketplaceNotificationService _notifications;

    public RequestBookingRefundCommandHandler(
        IPackageBookingRequestRepository requests,
        IPaymentGateway gateway,
        IMarketplaceNotificationService notifications)
    {
        _requests = requests;
        _gateway = gateway;
        _notifications = notifications;
    }

    public async Task<Result<BookingPaymentResponse>> Handle(RequestBookingRefundCommand command, CancellationToken cancellationToken)
    {
        var request = await _requests.GetByIdForTravelerAsync(command.RequestId, command.TravelerUserId, cancellationToken);
        if (request is null) return Result<BookingPaymentResponse>.Failure(PackageBookingErrors.NotFound);
        if (request.Status != PackageBookingRequestStatus.Confirmed)
            return Result<BookingPaymentResponse>.Failure(PackageBookingErrors.RefundNotAllowed);

        var payments = request.Payments
            .Where(item => item.Status == BookingPaymentStatus.Succeeded)
            .OrderByDescending(item => item.PaidAt)
            .ToList();
        if (payments.Count == 0) return Result<BookingPaymentResponse>.Failure(PackageBookingErrors.PaymentNotFound);

        var acceptedOffer = request.Offers.FirstOrDefault(item => item.Status == PackageBookingOfferStatus.Accepted);
        var refundPercentage = BookingCancellationPolicy.GetRefundPercentage(
            acceptedOffer?.CancellationPolicy,
            request.TravelPackage.StartDate,
            DateTime.UtcNow);
        try
        {
            foreach (var payment in payments)
            {
                var refundAmount = decimal.Round(
                    payment.GrossAmount * refundPercentage / 100m,
                    2,
                    MidpointRounding.AwayFromZero);
                if (refundAmount > 0)
                {
                    var refund = await _gateway.RefundPaymentAsync(new PaymentRefundRequest(
                        payment.Provider,
                        payment.PaymentSessionId,
                        refundAmount,
                        payment.CurrencyCode,
                        command.Reason.Trim()), cancellationToken);
                    payment.ProviderRefundId = refund.RefundId;
                }

                payment.Status = BookingPaymentStatus.Refunded;
                payment.RefundedAt = DateTime.UtcNow;
                payment.RefundedAmount = refundAmount;
                payment.RefundPercentage = refundPercentage;
            }
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("not configured", StringComparison.OrdinalIgnoreCase))
        {
            return Result<BookingPaymentResponse>.Failure(PackageBookingErrors.PaymentProviderNotConfigured);
        }

        var now = DateTime.UtcNow;
        request.Status = refundPercentage >= 100m
            ? PackageBookingRequestStatus.Refunded
            : PackageBookingRequestStatus.CancelledByTraveler;
        request.UpdatedAt = now;
        await _requests.SaveChangesAsync(cancellationToken);
        await _notifications.NotifyUserAsync(
            request.TravelPackage.TravelAgency.UserId,
            MarketplaceNotificationType.PackageBookingPaymentSucceeded,
            "Booking payment refunded",
            $"The payment for {request.TravelPackage.Title} was refunded to the traveler.",
            "/agency/booking-requests",
            cancellationToken);
        return Result<BookingPaymentResponse>.Success(BookingPaymentMapping.ToResponse(payments[0]));
    }
}

public sealed class CreateBookingPaymentCommandHandler
    : IRequestHandler<CreateBookingPaymentCommand, Result<BookingPaymentResponse>>
{
    private const decimal CommissionRate = 10m;
    private readonly IPackageBookingRequestRepository _requests;
    private readonly IPaymentGateway _gateway;

    public CreateBookingPaymentCommandHandler(IPackageBookingRequestRepository requests, IPaymentGateway gateway)
    {
        _requests = requests;
        _gateway = gateway;
    }

    public async Task<Result<BookingPaymentResponse>> Handle(CreateBookingPaymentCommand command, CancellationToken cancellationToken)
    {
        var request = await _requests.GetByIdForTravelerAsync(command.RequestId, command.TravelerUserId, cancellationToken);
        if (request is null) return Result<BookingPaymentResponse>.Failure(PackageBookingErrors.NotFound);

        var offer = request.Offers.FirstOrDefault(item => item.Status == PackageBookingOfferStatus.Accepted);
        if (offer is null) return Result<BookingPaymentResponse>.Failure(PackageBookingErrors.PaymentNotAllowed);
        if (offer.ExpiresAt <= DateTime.UtcNow)
        {
            offer.Status = PackageBookingOfferStatus.Expired;
            request.Status = PackageBookingRequestStatus.Expired;
            request.UpdatedAt = DateTime.UtcNow;
            await _requests.SaveChangesAsync(cancellationToken);
            return Result<BookingPaymentResponse>.Failure(PackageBookingErrors.OfferExpired);
        }

        var paidPayments = request.Payments
            .Where(payment => payment.Status == BookingPaymentStatus.Succeeded)
            .OrderBy(payment => payment.InstallmentNumber)
            .ToList();
        var paidAmount = paidPayments.Sum(payment => payment.GrossAmount);
        if (paidAmount >= offer.Amount)
            return Result<BookingPaymentResponse>.Failure(PackageBookingErrors.PaymentAlreadyComplete);

        var installmentNumber = paidPayments.Count + 1;
        if (installmentNumber == 1 && request.Status != PackageBookingRequestStatus.AwaitingPayment)
            return Result<BookingPaymentResponse>.Failure(PackageBookingErrors.PaymentNotAllowed);
        if (installmentNumber > 1 && request.Status != PackageBookingRequestStatus.Confirmed)
            return Result<BookingPaymentResponse>.Failure(PackageBookingErrors.PaymentNotAllowed);

        var finalPaymentDueAt = offer.FinalPaymentDueAt ?? request.TravelPackage.StartDate.AddDays(-14);
        if (installmentNumber > 1 && finalPaymentDueAt > DateTime.UtcNow)
            return Result<BookingPaymentResponse>.Failure(PackageBookingErrors.PaymentInstallmentNotDue);

        var now = DateTime.UtcNow;
        var installmentAmount = installmentNumber == 1
            ? decimal.Round(offer.Amount * offer.InitialPaymentPercentage / 100m, 2, MidpointRounding.AwayFromZero)
            : decimal.Round(offer.Amount - paidAmount, 2, MidpointRounding.AwayFromZero);
        installmentAmount = Math.Min(installmentAmount, offer.Amount - paidAmount);
        if (installmentAmount <= 0m)
            return Result<BookingPaymentResponse>.Failure(PackageBookingErrors.PaymentAlreadyComplete);

        var pendingPayment = request.Payments.FirstOrDefault(payment =>
            payment.Status == BookingPaymentStatus.Pending && payment.InstallmentNumber == installmentNumber);
        if (pendingPayment is not null)
        {
            if (Math.Abs(pendingPayment.GrossAmount - installmentAmount) < 0.01m)
            {
                try
                {
                    var checkoutUrl = await _gateway.GetOneTimeCheckoutUrlAsync(
                        pendingPayment.Provider,
                        pendingPayment.PaymentSessionId,
                        request.Id,
                        cancellationToken);
                    return Result<BookingPaymentResponse>.Success(BookingPaymentMapping.ToResponse(pendingPayment, checkoutUrl));
                }
                catch (InvalidOperationException exception) when (exception.Message.Contains("not configured", StringComparison.OrdinalIgnoreCase))
                {
                    return Result<BookingPaymentResponse>.Failure(PackageBookingErrors.PaymentProviderNotConfigured);
                }
            }

            pendingPayment.Status = BookingPaymentStatus.Failed;
            pendingPayment.FailureReason = "Superseded by the staged payment schedule.";
        }

        var commissionAmount = decimal.Round(installmentAmount * CommissionRate / 100m, 2, MidpointRounding.AwayFromZero);
        var payment = new BookingPayment
        {
            Id = Guid.NewGuid(),
            PackageBookingRequestId = request.Id,
            PackageBookingOfferId = offer.Id,
            PackageBookingRequest = request,
            PackageBookingOffer = offer,
            GrossAmount = installmentAmount,
            CommissionRate = CommissionRate,
            CommissionAmount = commissionAmount,
            AgencyAmount = installmentAmount - commissionAmount,
            CurrencyCode = offer.CurrencyCode.ToUpperInvariant(),
            InstallmentNumber = installmentNumber,
            DueAt = installmentNumber == 1 ? now : finalPaymentDueAt,
            Status = BookingPaymentStatus.Pending,
            CreatedAt = now
        };

        PaymentCheckoutSession checkout;
        try
        {
            checkout = await _gateway.CreateOneTimeCheckoutSessionAsync(new PaymentCheckoutRequest(
                request.TravelerUserId.ToString(),
                payment.GrossAmount,
                payment.CurrencyCode,
                $"Safarni booking installment {installmentNumber}: {request.TravelPackage.Title}",
                new Dictionary<string, string>
                {
                    ["bookingRequestId"] = request.Id.ToString(),
                    ["offerId"] = offer.Id.ToString(),
                    ["installmentNumber"] = installmentNumber.ToString(),
                    ["paymentType"] = "ProtectedBooking"
                },
                BillingPeriod.Monthly,
                request.TravelerUser.Email), cancellationToken);
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("not configured", StringComparison.OrdinalIgnoreCase))
        {
            return Result<BookingPaymentResponse>.Failure(PackageBookingErrors.PaymentProviderNotConfigured);
        }

        payment.Provider = checkout.Provider;
        payment.PaymentSessionId = checkout.SessionId;
        await _requests.AddPaymentAsync(payment, cancellationToken);
        await _requests.SaveChangesAsync(cancellationToken);
        return Result<BookingPaymentResponse>.Success(BookingPaymentMapping.ToResponse(payment, checkout.CheckoutUrl));
    }
}

public sealed class ProcessBookingPaymentWebhookCommandHandler
    : IRequestHandler<ProcessBookingPaymentWebhookCommand, Result<BookingPaymentResponse>>
{
    private readonly IPackageBookingRequestRepository _requests;
    private readonly IMarketplaceNotificationService _notifications;

    public ProcessBookingPaymentWebhookCommandHandler(
        IPackageBookingRequestRepository requests,
        IMarketplaceNotificationService notifications)
    {
        _requests = requests;
        _notifications = notifications;
    }

    public async Task<Result<BookingPaymentResponse>> Handle(ProcessBookingPaymentWebhookCommand command, CancellationToken cancellationToken)
    {
        var payment = await _requests.GetPaymentBySessionAsync(command.Event.Provider, command.Event.SessionId, cancellationToken);
        if (payment is null) return Result<BookingPaymentResponse>.Failure(PackageBookingErrors.PaymentNotFound);
        if (payment.ProviderEventId == command.Event.EventId)
            return Result<BookingPaymentResponse>.Success(BookingPaymentMapping.ToResponse(payment));
        if (payment.Status == BookingPaymentStatus.Failed &&
            payment.FailureReason == "Superseded by the staged payment schedule.")
            return Result<BookingPaymentResponse>.Success(BookingPaymentMapping.ToResponse(payment));

        var now = DateTime.UtcNow;
        payment.ProviderEventId = command.Event.EventId;
        payment.FailureReason = command.Event.FailureReason;
        payment.Status = command.Event.Status == PaymentStatus.Succeeded
            ? BookingPaymentStatus.Succeeded
            : BookingPaymentStatus.Failed;

        if (payment.Status == BookingPaymentStatus.Succeeded)
        {
            payment.PaidAt = now;
            payment.PackageBookingRequest.Status = PackageBookingRequestStatus.Confirmed;
            payment.PackageBookingRequest.UpdatedAt = now;
            var acceptedOffer = payment.PackageBookingOffer;
            if (acceptedOffer.AgencyServiceFeeAmount > 0 &&
                !payment.PackageBookingRequest.Payouts.Any(payout => payout.Type == BookingPayoutType.AgencyServiceFee))
            {
                var heldServiceFee = new BookingPayout
                {
                    Id = Guid.NewGuid(),
                    PackageBookingRequestId = payment.PackageBookingRequestId,
                    PackageBookingOfferId = payment.PackageBookingOfferId,
                    TravelAgencyId = payment.PackageBookingRequest.TravelPackage.TravelAgencyId,
                    Type = BookingPayoutType.AgencyServiceFee,
                    Status = BookingPayoutStatus.Held,
                    Amount = acceptedOffer.AgencyServiceFeeAmount,
                    CurrencyCode = payment.CurrencyCode,
                    ReleaseAt = payment.PackageBookingRequest.TravelPackage.EndDate.AddDays(3),
                    CreatedAt = now
                };
                await _requests.AddPayoutAsync(heldServiceFee, cancellationToken);
            }
            await _notifications.NotifyUserAsync(
                payment.PackageBookingRequest.TravelerUserId,
                MarketplaceNotificationType.PackageBookingPaymentSucceeded,
                "Booking payment confirmed",
                $"Your booking for {payment.PackageBookingRequest.TravelPackage.Title} is now confirmed.",
                "/agency-trips",
                cancellationToken);
            await _notifications.NotifyUserAsync(
                payment.PackageBookingRequest.TravelPackage.TravelAgency.UserId,
                MarketplaceNotificationType.PackageBookingPaymentSucceeded,
                "Booking payment received",
                $"Payment was received for {payment.PackageBookingRequest.TravelPackage.Title}. The booking is confirmed.",
                "/agency/booking-requests",
                cancellationToken);
        }
        else
        {
            await _notifications.NotifyUserAsync(
                payment.PackageBookingRequest.TravelerUserId,
                MarketplaceNotificationType.PackageBookingPaymentFailed,
                "Booking payment failed",
                $"Payment for {payment.PackageBookingRequest.TravelPackage.Title} was not completed. You can try again.",
                "/agency-trips",
                cancellationToken);
        }

        await _requests.SaveChangesAsync(cancellationToken);
        return Result<BookingPaymentResponse>.Success(BookingPaymentMapping.ToResponse(payment));
    }
}
