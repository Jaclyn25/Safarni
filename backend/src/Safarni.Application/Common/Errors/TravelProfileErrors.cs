namespace Safarni.Application.Common.Errors;

public static class TravelProfileErrors
{
    public static readonly Error NotFound = new(
        "TravelProfile.NotFound",
        "Travel preferences and passport information were not found.",
        ErrorType.NotFound);
}
