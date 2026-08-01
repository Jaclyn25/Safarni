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
}
