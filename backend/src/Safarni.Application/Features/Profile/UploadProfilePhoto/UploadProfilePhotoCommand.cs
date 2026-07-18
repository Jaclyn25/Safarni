using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Profile.UploadProfilePhoto;

public sealed record UploadProfilePhotoCommand(
    Guid UserId,
    string FileName,
    string ContentType,
    long FileSize,
    byte[] FileContent) : IRequest<Result<UploadProfilePhotoResponse>>;