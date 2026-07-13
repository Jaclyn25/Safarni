namespace Safarni.Application.Common.Errors;

public static class VisaApplicationErrors
{
    public static readonly Error NotFound = new(
        "VisaApplication.NotFound",
        "Visa application was not found.",
        ErrorType.NotFound);
}
