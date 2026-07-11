namespace Safarni.Application.Features.Auth.RefreshTokens;

public sealed record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);
