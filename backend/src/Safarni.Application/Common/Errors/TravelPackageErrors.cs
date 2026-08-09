namespace Safarni.Application.Common.Errors;

public static class TravelPackageErrors
{
    public static readonly Error NotFound = new(
        "TravelPackage.NotFound",
        "The travel package could not be found.",
        ErrorType.NotFound);

    public static readonly Error OwnershipDenied = new(
        "TravelPackage.OwnershipDenied",
        "You are not allowed to manage this travel package.",
        ErrorType.Forbidden);

    public static readonly Error PublishingRequiresVerifiedAgency = new(
        "TravelPackage.VerifiedAgencyRequired",
        "Only a verified travel agency can publish packages.",
        ErrorType.Forbidden);

    public static readonly Error AlreadyPublished = new(
        "TravelPackage.AlreadyPublished",
        "The travel package is already published.",
        ErrorType.Conflict);

    public static readonly Error AlreadyUnpublished = new(
        "TravelPackage.AlreadyUnpublished",
        "The travel package is already unpublished.",
        ErrorType.Conflict);

    public static readonly Error AlreadyInactive = new(
        "TravelPackage.AlreadyInactive",
        "The travel package is already inactive.",
        ErrorType.Conflict);

    public static readonly Error DayNotFound = new(
        "TravelPackage.DayNotFound",
        "The requested package day could not be found.",
        ErrorType.NotFound);

    public static readonly Error InvalidDayNumber = new(
        "TravelPackage.InvalidDayNumber",
        "The day number must be within the package duration.",
        ErrorType.Validation);

    public static readonly Error DuplicateDayNumber = new(
        "TravelPackage.DuplicateDayNumber",
        "Each package day number can only be used once.",
        ErrorType.Conflict);

    public static readonly Error InvalidPackageImage = new(
        "TravelPackage.InvalidImage",
        "The package image must be a supported image under 5 MB.",
        ErrorType.Validation);
}
