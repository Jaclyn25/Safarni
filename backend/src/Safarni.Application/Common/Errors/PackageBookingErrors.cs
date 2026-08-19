namespace Safarni.Application.Common.Errors;

public static class PackageBookingErrors
{
    public static readonly Error NotFound = new(
        "PackageBooking.NotFound",
        "The package booking request could not be found.",
        ErrorType.NotFound);

    public static readonly Error PackageUnavailable = new(
        "PackageBooking.PackageUnavailable",
        "This package is not available for booking.",
        ErrorType.Conflict);

    public static readonly Error PackageExpired = new(
        "PackageBooking.PackageExpired",
        "This package has already expired.",
        ErrorType.Conflict);

    public static readonly Error PackageFull = new(
        "PackageBooking.PackageFull",
        "This package does not have enough available seats.",
        ErrorType.Conflict);

    public static readonly Error DuplicateRequest = new(
        "PackageBooking.DuplicateRequest",
        "You already have an active request for this package.",
        ErrorType.Conflict);

    public static readonly Error ReviewNotAllowed = new(
        "AgencyReview.NotAllowed",
        "You can review an agency after your accepted trip has ended.",
        ErrorType.Forbidden);

    public static readonly Error AlreadyReviewed = new(
        "AgencyReview.AlreadyReviewed",
        "This booking has already been reviewed.",
        ErrorType.Conflict);

    public static readonly Error OwnershipDenied = new(
        "PackageBooking.OwnershipDenied",
        "You are not allowed to manage this booking request.",
        ErrorType.Forbidden);

    public static readonly Error InvalidStatus = new(
        "PackageBooking.InvalidStatus",
        "Only pending booking requests can be processed.",
        ErrorType.Conflict);

    public static readonly Error OfferNotFound = new(
        "PackageBooking.OfferNotFound",
        "The booking offer could not be found.",
        ErrorType.NotFound);

    public static readonly Error OfferExpired = new(
        "PackageBooking.OfferExpired",
        "This booking offer has expired.",
        ErrorType.Conflict);

    public static readonly Error OfferNotAcceptable = new(
        "PackageBooking.OfferNotAcceptable",
        "This booking offer is no longer available for acceptance.",
        ErrorType.Conflict);

    public static readonly Error PaymentNotAllowed = new(
        "PackageBooking.PaymentNotAllowed",
        "This booking is not ready for payment.",
        ErrorType.Conflict);

    public static readonly Error PaymentInstallmentNotDue = new(
        "PackageBooking.PaymentInstallmentNotDue",
        "The remaining booking installment is not due yet.",
        ErrorType.Conflict);

    public static readonly Error PaymentAlreadyComplete = new(
        "PackageBooking.PaymentAlreadyComplete",
        "This booking has already been paid in full.",
        ErrorType.Conflict);

    public static readonly Error PaymentNotFound = new(
        "PackageBooking.PaymentNotFound",
        "Booking payment was not found.",
        ErrorType.NotFound);

    public static readonly Error PaymentProviderNotConfigured = new(
        "PackageBooking.PaymentProviderNotConfigured",
        "The booking payment provider is not configured.",
        ErrorType.Validation);

    public static readonly Error RefundNotAllowed = new(
        "PackageBooking.RefundNotAllowed",
        "This booking cannot be refunded at this stage.",
        ErrorType.Conflict);

    public static readonly Error SupplierEvidenceNotAllowed = new(
        "PackageBooking.SupplierEvidenceNotAllowed",
        "Supplier evidence can only be submitted for a confirmed booking.",
        ErrorType.Conflict);

    public static readonly Error PayoutProfileNotVerified = new(
        "PackageBooking.PayoutProfileNotVerified",
        "The agency must have verified payout details before a payout can be released.",
        ErrorType.Conflict);

    public static readonly Error PayoutNotAllowed = new(
        "PackageBooking.PayoutNotAllowed",
        "This payout is not ready for the requested action.",
        ErrorType.Conflict);

    public static readonly Error DisputeNotAllowed = new(
        "PackageBooking.DisputeNotAllowed",
        "A dispute cannot be opened or changed for this booking at its current stage.",
        ErrorType.Conflict);
}
