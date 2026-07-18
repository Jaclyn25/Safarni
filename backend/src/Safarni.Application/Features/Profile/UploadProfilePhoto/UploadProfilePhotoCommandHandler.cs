using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Storage;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Profile.UploadProfilePhoto;

public sealed class UploadProfilePhotoCommandHandler : IRequestHandler<UploadProfilePhotoCommand, Result<UploadProfilePhotoResponse>>
{
    private static readonly HashSet<string> AllowedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    ];

    private readonly IUserRepository _userRepository;
    private readonly IProfilePhotoStorage _profilePhotoStorage;

    public UploadProfilePhotoCommandHandler(IUserRepository userRepository, IProfilePhotoStorage profilePhotoStorage)
    {
        _userRepository = userRepository;
        _profilePhotoStorage = profilePhotoStorage;
    }

    public async Task<Result<UploadProfilePhotoResponse>> Handle(UploadProfilePhotoCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result<UploadProfilePhotoResponse>.Failure(UserErrors.NotFound);
        }

        if (request.FileContent.LongLength != request.FileSize)
        {
            return Result<UploadProfilePhotoResponse>.Failure(UserErrors.InvalidProfilePhoto);
        }

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            return Result<UploadProfilePhotoResponse>.Failure(UserErrors.InvalidProfilePhoto);
        }

        await _profilePhotoStorage.DeleteAsync(user.ProfilePhotoUrl, cancellationToken);

        await using var fileStream = new MemoryStream(request.FileContent);
        var photoUrl = await _profilePhotoStorage.SaveAsync(fileStream, extension, cancellationToken);

        user.ProfilePhotoUrl = photoUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<UploadProfilePhotoResponse>.Success(new UploadProfilePhotoResponse(
            user.Id,
            user.ProfilePhotoUrl,
            user.UpdatedAt.Value));
    }
}
