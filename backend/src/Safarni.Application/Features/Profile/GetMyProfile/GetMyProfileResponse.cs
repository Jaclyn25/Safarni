namespace Safarni.Application.Features.Profile.GetMyProfile;

public sealed record GetMyProfileResponse(
    Guid UserId,
    string FullName,
    string Email,
    string? PhoneNumber,
    DateTime? DateOfBirth,
    string? Country,
    string? City,
    string? Bio,
    string? ProfilePhotoUrl,
    DateTime CreatedAt,
    DateTime? UpdatedAt);