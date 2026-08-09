using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Storage;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Packages;

public sealed record UploadTravelPackageImageCommand(
    Guid UserId,
    Guid PackageId,
    string FileName,
    string ContentType,
    long FileSize,
    byte[] FileContent)
    : IRequest<Result<TravelPackageResponse>>;

public sealed class UploadTravelPackageImageCommandHandler
    : IRequestHandler<UploadTravelPackageImageCommand, Result<TravelPackageResponse>>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;
    private readonly ITravelPackageImageStorage _storage;

    public UploadTravelPackageImageCommandHandler(
        ITravelAgencyRepository agencies,
        ITravelPackageRepository packages,
        ITravelPackageImageStorage storage)
    {
        _agencies = agencies;
        _packages = packages;
        _storage = storage;
    }

    public async Task<Result<TravelPackageResponse>> Handle(UploadTravelPackageImageCommand request, CancellationToken cancellationToken)
    {
        if (request.FileSize <= 0 || request.FileSize > 5 * 1024 * 1024 ||
            !AllowedContentTypes.Contains(request.ContentType))
        {
            return Result<TravelPackageResponse>.Failure(TravelPackageErrors.InvalidPackageImage);
        }

        var agency = await _agencies.GetByUserIdAsync(request.UserId, cancellationToken: cancellationToken);
        if (agency is null) return Result<TravelPackageResponse>.Failure(TravelAgencyErrors.AgencyAccountRequired);
        var package = await _packages.GetByIdForAgencyAsync(request.PackageId, agency.Id, includeDays: true, cancellationToken: cancellationToken);
        if (package is null)
        {
            return await _packages.GetByIdAsync(request.PackageId, cancellationToken: cancellationToken) is null
                ? Result<TravelPackageResponse>.Failure(TravelPackageErrors.NotFound)
                : Result<TravelPackageResponse>.Failure(TravelPackageErrors.OwnershipDenied);
        }

        var extension = Path.GetExtension(request.FileName);
        if (string.IsNullOrWhiteSpace(extension))
            extension = request.ContentType switch { "image/png" => ".png", "image/webp" => ".webp", _ => ".jpg" };

        string newImageUrl;
        await using (var stream = new MemoryStream(request.FileContent, writable: false))
        {
            newImageUrl = await _storage.SaveAsync(stream, extension, cancellationToken);
        }
        var previousImageUrl = package.CoverImageUrl;
        package.CoverImageUrl = newImageUrl;
        package.UpdatedAt = DateTime.UtcNow;
        await _packages.SaveChangesAsync(cancellationToken);
        await _storage.DeleteAsync(previousImageUrl, cancellationToken);
        return Result<TravelPackageResponse>.Success(TravelPackageMapping.ToResponse(package));
    }
}
