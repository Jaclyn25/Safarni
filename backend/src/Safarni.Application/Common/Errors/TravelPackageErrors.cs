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

    public static readonly Error CityNotFound = new(
        "TravelPackage.CityNotFound",
        "The selected package city could not be found.",
        ErrorType.NotFound);

    public static readonly Error ItemNotFound = new(
        "TravelPackage.ItemNotFound",
        "The selected itinerary item could not be found.",
        ErrorType.NotFound);

    public static readonly Error ImageNotFound = new(
        "TravelPackage.ImageNotFound",
        "The selected image could not be found.",
        ErrorType.NotFound);

    public static readonly Error IncompleteProgram = new(
        "TravelPackage.IncompleteProgram",
        "Complete the route and every itinerary day before publishing.",
        ErrorType.Validation);

    public static readonly Error RouteRequired = new(
        "TravelPackage.RouteRequired",
        "Add at least one route city before creating itinerary days.",
        ErrorType.Validation);

    public static readonly Error InvalidNightAllocation = new(
        "TravelPackage.InvalidNightAllocation",
        "Route nights must equal the trip days, or one less when the final day is the departure day. Adjust the route nights to match the package duration.",
        ErrorType.Validation);

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
