using FluentValidation;
using MediatR;
using Safarni.Application.Abstractions.Notifications;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Packages;

public sealed record PackageBookingOfferResponse(
    Guid Id,
    Guid BookingRequestId,
    int Version,
    decimal Amount,
    decimal SupplierCostAmount,
    decimal AgencyServiceFeeAmount,
    decimal SafarniProtectionFeeAmount,
    string CurrencyCode,
    int TravelerCount,
    string? Included,
    string? Excluded,
    string? CancellationPolicy,
    string? AgencyNote,
    PackageBookingOfferStatus Status,
    DateTime ExpiresAt,
    DateTime CreatedAt,
    DateTime? AcceptedAt,
    decimal InitialPaymentPercentage,
    DateTime? FinalPaymentDueAt);

public sealed record SendPackageBookingOfferCommand(
    Guid AgencyUserId,
    Guid RequestId,
    decimal Amount,
    decimal? SupplierCostAmount,
    decimal? AgencyServiceFeeAmount,
    decimal? SafarniProtectionFeeAmount,
    string CurrencyCode,
    DateTime ExpiresAt,
    string? Included,
    string? Excluded,
    string? CancellationPolicy,
    string? AgencyNote,
    decimal? InitialPaymentPercentage = null,
    DateTime? FinalPaymentDueAt = null)
    : IRequest<Result<PackageBookingRequestResponse>>;

public sealed record AcceptPackageBookingOfferCommand(
    Guid TravelerUserId,
    Guid RequestId,
    Guid OfferId)
    : IRequest<Result<PackageBookingRequestResponse>>;

public sealed class SendPackageBookingOfferCommandValidator
    : AbstractValidator<SendPackageBookingOfferCommand>
{
    public SendPackageBookingOfferCommandValidator()
    {
        RuleFor(command => command.AgencyUserId).NotEmpty();
        RuleFor(command => command.RequestId).NotEmpty();
        RuleFor(command => command.Amount).GreaterThanOrEqualTo(0);
        RuleFor(command => command.SupplierCostAmount).GreaterThanOrEqualTo(0);
        RuleFor(command => command.AgencyServiceFeeAmount).GreaterThanOrEqualTo(0);
        RuleFor(command => command.SafarniProtectionFeeAmount).GreaterThanOrEqualTo(0);
        RuleFor(command => command).Must(command =>
                command.SupplierCostAmount is null && command.AgencyServiceFeeAmount is null && command.SafarniProtectionFeeAmount is null ||
                Math.Abs((command.SupplierCostAmount ?? 0) + (command.AgencyServiceFeeAmount ?? 0) + (command.SafarniProtectionFeeAmount ?? 0) - command.Amount) < 0.01m)
            .WithMessage("The payout breakdown must equal the total offer amount.");
        RuleFor(command => command.CurrencyCode)
            .NotEmpty()
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency code must be a three-letter ISO code.");
        RuleFor(command => command.ExpiresAt)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Offer expiry must be in the future.");
        RuleFor(command => command.Included).MaximumLength(4000);
        RuleFor(command => command.Excluded).MaximumLength(4000);
        RuleFor(command => command.CancellationPolicy).MaximumLength(4000);
        RuleFor(command => command.AgencyNote).MaximumLength(2000);
        RuleFor(command => command.InitialPaymentPercentage)
            .InclusiveBetween(1m, 99m)
            .When(command => command.InitialPaymentPercentage is not null);
    }
}

public sealed class AcceptPackageBookingOfferCommandValidator
    : AbstractValidator<AcceptPackageBookingOfferCommand>
{
    public AcceptPackageBookingOfferCommandValidator()
    {
        RuleFor(command => command.TravelerUserId).NotEmpty();
        RuleFor(command => command.RequestId).NotEmpty();
        RuleFor(command => command.OfferId).NotEmpty();
    }
}

internal static class PackageBookingOfferMapping
{
    public static PackageBookingOfferResponse ToResponse(PackageBookingOffer offer) => new(
        offer.Id,
        offer.PackageBookingRequestId,
        offer.Version,
        offer.Amount,
        offer.SupplierCostAmount,
        offer.AgencyServiceFeeAmount,
        offer.SafarniProtectionFeeAmount,
        offer.CurrencyCode,
        offer.TravelerCount,
        offer.Included,
        offer.Excluded,
        offer.CancellationPolicy,
        offer.AgencyNote,
        offer.Status,
        offer.ExpiresAt,
        offer.CreatedAt,
        offer.AcceptedAt,
        offer.InitialPaymentPercentage,
        offer.FinalPaymentDueAt);
}

public sealed class SendPackageBookingOfferCommandHandler
    : IRequestHandler<SendPackageBookingOfferCommand, Result<PackageBookingRequestResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly IPackageBookingRequestRepository _requests;
    private readonly IMarketplaceNotificationService _notifications;

    public SendPackageBookingOfferCommandHandler(
        ITravelAgencyRepository agencies,
        IPackageBookingRequestRepository requests,
        IMarketplaceNotificationService notifications)
    {
        _agencies = agencies;
        _requests = requests;
        _notifications = notifications;
    }

    public async Task<Result<PackageBookingRequestResponse>> Handle(
        SendPackageBookingOfferCommand command,
        CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(
            command.AgencyUserId,
            cancellationToken: cancellationToken);
        if (agency is null)
        {
            return Result<PackageBookingRequestResponse>.Failure(
                TravelAgencyErrors.AgencyAccountRequired);
        }

        var request = await _requests.GetByIdForAgencyAsync(
            command.RequestId,
            agency.Id,
            cancellationToken);
        if (request is null)
        {
            return Result<PackageBookingRequestResponse>.Failure(PackageBookingErrors.NotFound);
        }

        if (request.Status is not (PackageBookingRequestStatus.Pending or PackageBookingRequestStatus.OfferSent))
        {
            return Result<PackageBookingRequestResponse>.Failure(PackageBookingErrors.InvalidStatus);
        }

        var now = DateTime.UtcNow;
        foreach (var previousOffer in request.Offers.Where(offer => offer.Status == PackageBookingOfferStatus.Sent))
        {
            previousOffer.Status = PackageBookingOfferStatus.Superseded;
            previousOffer.SupersededAt = now;
        }

        var offer = new PackageBookingOffer
        {
            Id = Guid.NewGuid(),
            PackageBookingRequestId = request.Id,
            Version = request.Offers.Count == 0 ? 1 : request.Offers.Max(item => item.Version) + 1,
            Amount = command.Amount,
            SupplierCostAmount = command.SupplierCostAmount ?? command.Amount,
            AgencyServiceFeeAmount = command.AgencyServiceFeeAmount ?? 0m,
            SafarniProtectionFeeAmount = command.SafarniProtectionFeeAmount ?? 0m,
            InitialPaymentPercentage = command.InitialPaymentPercentage ?? 30m,
            FinalPaymentDueAt = command.FinalPaymentDueAt ?? request.TravelPackage.StartDate.AddDays(-14),
            CurrencyCode = command.CurrencyCode.Trim().ToUpperInvariant(),
            TravelerCount = request.TravelerCount,
            Included = TrimToNull(command.Included),
            Excluded = TrimToNull(command.Excluded),
            CancellationPolicy = TrimToNull(command.CancellationPolicy),
            AgencyNote = TrimToNull(command.AgencyNote),
            Status = PackageBookingOfferStatus.Sent,
            ExpiresAt = command.ExpiresAt,
            CreatedAt = now
        };

        request.Offers.Add(offer);
        request.Status = PackageBookingRequestStatus.OfferSent;
        request.UpdatedAt = now;
        await _requests.AddOfferAsync(offer, cancellationToken);
        await _requests.SaveChangesAsync(cancellationToken);

        await _notifications.NotifyUserAsync(
            request.TravelerUserId,
            MarketplaceNotificationType.PackageBookingOfferReceived,
            "New booking offer",
            $"{agency.CompanyName} sent you an offer for {request.TravelPackage.Title}.",
            "/profile/agency-trips",
            cancellationToken);

        return Result<PackageBookingRequestResponse>.Success(
            PackageBookingMapping.ToResponse(request));
    }

    private static string? TrimToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class AcceptPackageBookingOfferCommandHandler
    : IRequestHandler<AcceptPackageBookingOfferCommand, Result<PackageBookingRequestResponse>>
{
    private readonly IPackageBookingRequestRepository _requests;
    private readonly IMarketplaceNotificationService _notifications;

    public AcceptPackageBookingOfferCommandHandler(
        IPackageBookingRequestRepository requests,
        IMarketplaceNotificationService notifications)
    {
        _requests = requests;
        _notifications = notifications;
    }

    public async Task<Result<PackageBookingRequestResponse>> Handle(
        AcceptPackageBookingOfferCommand command,
        CancellationToken cancellationToken)
    {
        var request = await _requests.GetByIdForTravelerAsync(
            command.RequestId,
            command.TravelerUserId,
            cancellationToken);
        if (request is null)
        {
            return Result<PackageBookingRequestResponse>.Failure(PackageBookingErrors.NotFound);
        }

        var offer = request.Offers.FirstOrDefault(item => item.Id == command.OfferId);
        if (offer is null)
        {
            return Result<PackageBookingRequestResponse>.Failure(PackageBookingErrors.OfferNotFound);
        }

        if (offer.Status != PackageBookingOfferStatus.Sent ||
            request.Status != PackageBookingRequestStatus.OfferSent)
        {
            return Result<PackageBookingRequestResponse>.Failure(PackageBookingErrors.OfferNotAcceptable);
        }

        var now = DateTime.UtcNow;
        if (offer.ExpiresAt <= now)
        {
            offer.Status = PackageBookingOfferStatus.Expired;
            request.Status = PackageBookingRequestStatus.Expired;
            request.UpdatedAt = now;
            await _requests.SaveChangesAsync(cancellationToken);
            return Result<PackageBookingRequestResponse>.Failure(PackageBookingErrors.OfferExpired);
        }

        offer.Status = PackageBookingOfferStatus.Accepted;
        offer.AcceptedAt = now;
        request.Status = PackageBookingRequestStatus.AwaitingPayment;
        request.UpdatedAt = now;
        await _requests.SaveChangesAsync(cancellationToken);

        await _notifications.NotifyUserAsync(
            request.TravelPackage.TravelAgency.UserId,
            MarketplaceNotificationType.PackageBookingOfferAccepted,
            "Booking offer accepted",
            $"{request.TravelerUser.FullName} accepted the offer for {request.TravelPackage.Title}.",
            "/agency/booking-requests",
            cancellationToken);

        return Result<PackageBookingRequestResponse>.Success(
            PackageBookingMapping.ToResponse(request));
    }
}
