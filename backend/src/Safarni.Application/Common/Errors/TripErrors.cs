namespace Safarni.Application.Common.Errors;

public static class TripErrors
{
    public static readonly Error NotFound = new(
        "Trip.NotFound",
        "The requested trip was not found.",
        ErrorType.NotFound);

    public static readonly Error Forbidden = new(
        "Trip.Forbidden",
        "You do not have permission to access this trip.",
        ErrorType.Forbidden);

    public static readonly Error CorruptRoute = new(
        "Trip.CorruptRoute",
        "The trip city route is invalid and must be edited before generating an itinerary.",
        ErrorType.Conflict);

    public static readonly Error NotCompleted = new(
        "Trip.NotCompleted",
        "Trip must be marked complete before it can be published.",
        ErrorType.Conflict);

    public static readonly Error AlreadyPublished = new(
        "Trip.AlreadyPublished",
        "Trip is already published. Use republish instead.",
        ErrorType.Conflict);

    public static readonly Error PersonalInfoNotConfirmed = new(
        "Trip.PersonalInfoNotConfirmed",
        "You must confirm that companion names and personal details are not exposed.",
        ErrorType.Conflict);
    public static readonly Error NotPublished = new(
    "Trip.NotPublished",
    "Trip must be published before its visibility can be changed.",
    ErrorType.Conflict);
}