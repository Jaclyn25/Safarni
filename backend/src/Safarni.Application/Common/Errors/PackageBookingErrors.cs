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
}
