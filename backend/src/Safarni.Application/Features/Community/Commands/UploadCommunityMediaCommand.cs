using MediatR;
using Safarni.Application.Abstractions.Storage;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Community.Commands;

public sealed record UploadCommunityMediaCommand(
    string FileName,
    string ContentType,
    long FileSize,
    byte[] FileContent) : IRequest<Result<UploadCommunityMediaResponse>>;

public sealed record UploadCommunityMediaResponse(
    string MediaType,
    string Url,
    string? ThumbnailUrl,
    string AltText);

public sealed class UploadCommunityMediaCommandHandler
    : IRequestHandler<UploadCommunityMediaCommand, Result<UploadCommunityMediaResponse>>
{
    private static readonly HashSet<string> AllowedImageExtensions =
    [
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    ];

    private static readonly HashSet<string> AllowedVideoExtensions =
    [
        ".mp4", ".webm", ".mov"
    ];

    private const long MaxImageSizeBytes = 10 * 1024 * 1024;
    private const long MaxVideoSizeBytes = 60 * 1024 * 1024;

    private readonly ICommunityMediaStorage _mediaStorage;

    public UploadCommunityMediaCommandHandler(ICommunityMediaStorage mediaStorage)
    {
        _mediaStorage = mediaStorage;
    }

    public async Task<Result<UploadCommunityMediaResponse>> Handle(
        UploadCommunityMediaCommand request,
        CancellationToken cancellationToken)
    {
        if (request.FileContent.LongLength != request.FileSize || request.FileSize <= 0)
        {
            return InvalidMedia();
        }

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        var isImage = AllowedImageExtensions.Contains(extension);
        var isVideo = AllowedVideoExtensions.Contains(extension);

        if ((!isImage && !isVideo) ||
            (isImage && !request.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) ||
            (isVideo && !request.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)))
        {
            return InvalidMedia();
        }

        var maxSize = isImage ? MaxImageSizeBytes : MaxVideoSizeBytes;
        if (request.FileSize > maxSize)
        {
            return Result<UploadCommunityMediaResponse>.Failure(
                new Error(
                    "Community.MediaTooLarge",
                    isImage ? "Each image must be 10 MB or smaller." : "Each video must be 60 MB or smaller.",
                    ErrorType.Validation));
        }

        await using var fileStream = new MemoryStream(request.FileContent, writable: false);
        var url = await _mediaStorage.SaveAsync(fileStream, extension, cancellationToken);

        return Result<UploadCommunityMediaResponse>.Success(
            new UploadCommunityMediaResponse(
                isVideo ? "video" : "image",
                url,
                null,
                Path.GetFileNameWithoutExtension(request.FileName)));
    }

    private static Result<UploadCommunityMediaResponse> InvalidMedia() =>
        Result<UploadCommunityMediaResponse>.Failure(
            new Error(
                "Community.InvalidMedia",
                "Choose a valid image or video file.",
                ErrorType.Validation));
}
