namespace Safarni.Application.Features.Auth.Register;

public sealed record RegisterUserResponse(
    Guid Id,
    string FullName,
    string Email,
    DateTime CreatedAt,
    bool IsActive);