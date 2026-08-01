namespace Safarni.Application.Common.Errors;

public static class ItineraryErrors
{
    public static readonly Error TripNotFound = new(
        "Itinerary.TripNotFound",
        "The trip was not found.",
        ErrorType.NotFound);

    public static readonly Error NotFound = new(
        "Itinerary.NotFound",
        "No active itinerary was found for this trip.",
        ErrorType.NotFound);

    public static readonly Error DayNotFound = new(
        "Itinerary.DayNotFound",
        "The itinerary day was not found.",
        ErrorType.NotFound);

    public static readonly Error ItemNotFound = new(
        "Itinerary.ItemNotFound",
        "The itinerary item was not found.",
        ErrorType.NotFound);

    public static readonly Error VenueNotFound = new(
        "Itinerary.VenueNotFound",
        "The selected venue is no longer available in the verified results.",
        ErrorType.NotFound);

    public static readonly Error UnsupportedVenueSelection = new(
        "Itinerary.UnsupportedVenueSelection",
        "Venue choices are available only for meals and accommodation.",
        ErrorType.Validation);

    public static readonly Error Forbidden = new(
        "Itinerary.Forbidden",
        "You cannot access or change this trip itinerary.",
        ErrorType.Forbidden);

    public static readonly Error PreferencesRequired = new(
        "Itinerary.PreferencesRequired",
        "Travel preferences are required.",
        ErrorType.Validation);

    public static readonly Error PreferencesTooLong = new(
        "Itinerary.PreferencesTooLong",
        "Travel preferences cannot exceed 1000 characters.",
        ErrorType.Validation);

    public static readonly Error InvalidAiResponse = new(
        "Itinerary.InvalidAiResponse",
        "The AI returned an invalid itinerary structure. Please try again.",
        ErrorType.Failure);

    public static readonly Error GenerationFailed = new(
        "Itinerary.GenerationFailed",
        "The itinerary could not be generated. Please try again.",
        ErrorType.Failure);

    public static readonly Error BudgetInsufficientForFlights = new(
        "Itinerary.BudgetInsufficientForFlights",
        "The selected budget does not cover the verified flight prices for this route.",
        ErrorType.Validation);

    public static readonly Error GenerationAlreadyRunning = new(
        "Itinerary.GenerationAlreadyRunning",
        "An itinerary generation job is already queued or running for this trip.",
        ErrorType.Conflict);

    public static readonly Error IdempotencyKeyReused = new(
        "Itinerary.IdempotencyKeyReused",
        "This idempotency key was already used for a different trip request.",
        ErrorType.Conflict);

    public static readonly Error GenerationJobNotFound = new(
        "Itinerary.GenerationJobNotFound",
        "The itinerary generation job was not found.",
        ErrorType.NotFound);

    public static readonly Error InvalidItem = new(
        "Itinerary.InvalidItem",
        "The itinerary item contains invalid values.",
        ErrorType.Validation);

    public static readonly Error IncompleteAiResponse = new(
    "Itinerary.IncompleteAiResponse",
    "The AI response stopped before completing all trip days. Please regenerate the itinerary.",
    ErrorType.Failure);

    public static readonly Error PlacesUnavailable = new(
    "Itinerary.PlacesUnavailable",
    "Real places could not be loaded from Google Places. Please try again.",
    ErrorType.Failure);
}
