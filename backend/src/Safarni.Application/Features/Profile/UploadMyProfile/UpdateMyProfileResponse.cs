namespace Safarni.Application.Features.Profile.UpdateMyProfile;

public sealed record UpdateMyProfileResponse(
    Guid UserId,
    string FullName,
    string Email,
    string? PhoneNumber,
    DateTime? DateOfBirth,
    string? Country,
    string? City,
    string? Bio,
    string? ProfilePhotoUrl,
    DateTime UpdatedAt);
