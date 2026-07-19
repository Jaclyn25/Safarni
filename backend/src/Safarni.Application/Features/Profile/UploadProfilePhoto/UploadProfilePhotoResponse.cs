namespace Safarni.Application.Features.Profile.UploadProfilePhoto;

public sealed record UploadProfilePhotoResponse(
    Guid UserId,
    string ProfilePhotoUrl,
    DateTime UpdatedAt);
