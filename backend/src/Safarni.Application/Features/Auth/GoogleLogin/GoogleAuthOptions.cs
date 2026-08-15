namespace Safarni.Application.Features.Auth.GoogleLogin;

public sealed record GoogleAuthOptions(
    string ClientId,
    IReadOnlyCollection<string> AllowedClientIds);
