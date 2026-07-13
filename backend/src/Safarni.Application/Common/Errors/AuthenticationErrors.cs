namespace Safarni.Application.Common.Errors;

public static class AuthenticationErrors
{
    public static readonly Error AuthenticationRequired = new(
        "Auth.AuthenticationRequired",
        "An authenticated user is required.",
        ErrorType.Unauthorized);

    public static readonly Error InvalidCredentials = new(
        "Auth.InvalidCredentials",
        "The provided credentials are invalid.",
        ErrorType.Unauthorized);

    public static readonly Error InactiveAccount = new(
        "Auth.InactiveAccount",
        "The account is inactive.",
        ErrorType.Forbidden);

    public static readonly Error InvalidRefreshToken = new(
        "Auth.InvalidRefreshToken",
        "The refresh token is invalid.",
        ErrorType.Unauthorized);

    public static readonly Error RefreshTokenNotFound = new(
        "Auth.RefreshTokenNotFound",
        "The refresh token was not found.",
        ErrorType.NotFound);
}
