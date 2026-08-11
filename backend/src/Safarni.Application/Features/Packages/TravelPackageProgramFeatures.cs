using FluentValidation;
using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Storage;
using Safarni.Application.Abstractions.Travel;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Packages;

public sealed record SaveTravelPackageCityCommand(
    Guid UserId,
    Guid PackageId,
    Guid? CityId,
    string Name,
    int Nights,
    string? Description) : IRequest<Result<TravelPackageResponse>>;

public sealed record DeleteTravelPackageCityCommand(
    Guid UserId,
    Guid PackageId,
    Guid CityId) : IRequest<Result<TravelPackageResponse>>;

public sealed record TravelPackageItemInput(
    TravelPackageItemType Type,
    TravelPackageItemSource Source,
    int OrderIndex,
    string Title,
    string? Description,
    string? LocationName,
    string? Address,
    double? Latitude,
    double? Longitude,
    string? MapUrl,
    string? Provider,
    string? ProviderPlaceId,
    string? ProviderPhotoUrl,
    string? WebsiteUrl,
    string? Cuisine,
    string? MealType,
    bool? ReservationRequired,
    string? AccommodationType,
    int? StarRating,
    string? RoomType,
    string? BoardBasis,
    string? TransportMode,
    string? FromLocation,
    string? ToLocation,
    string? MeetingPoint,
    string? DifficultyLevel,
    string? AdditionalDetails);

public sealed record SaveTravelPackageItemCommand(
    Guid UserId,
    Guid PackageId,
    Guid DayId,
    Guid? ItemId,
    TravelPackageItemInput Item) : IRequest<Result<TravelPackageResponse>>;

public sealed record DeleteTravelPackageItemCommand(
    Guid UserId,
    Guid PackageId,
    Guid DayId,
    Guid ItemId) : IRequest<Result<TravelPackageResponse>>;

public sealed record PackagePlaceSearchResult(
    string Provider,
    string ProviderPlaceId,
    string Name,
    string Address,
    string? Description,
    double? Rating,
    int? ReviewCount,
    double? Latitude,
    double? Longitude,
    string? MapUrl,
    string? PhotoUrl,
    string? WebsiteUrl);

public sealed record SearchPackagePlacesQuery(
    Guid UserId,
    Guid PackageId,
    Guid DayId,
    string Query,
    TravelPackageItemType Type) : IRequest<Result<IReadOnlyList<PackagePlaceSearchResult>>>;

public sealed record UploadTravelPackageGalleryImageCommand(
    Guid UserId,
    Guid PackageId,
    Guid? DayId,
    Guid? ItemId,
    string? Caption,
    string? AltText,
    bool IsCover,
    string FileName,
    string ContentType,
    long FileSize,
    byte[] FileContent) : IRequest<Result<TravelPackageResponse>>;

public sealed record DeleteTravelPackageGalleryImageCommand(
    Guid UserId,
    Guid PackageId,
    Guid ImageId) : IRequest<Result<TravelPackageResponse>>;

public sealed class SaveTravelPackageCityCommandValidator : AbstractValidator<SaveTravelPackageCityCommand>
{
    public SaveTravelPackageCityCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(150);
        RuleFor(command => command.Nights).InclusiveBetween(1, 365);
        RuleFor(command => command.Description).MaximumLength(1000);
    }
}

public sealed class SaveTravelPackageItemCommandValidator : AbstractValidator<SaveTravelPackageItemCommand>
{
    public SaveTravelPackageItemCommandValidator()
    {
        RuleFor(command => command.DayId).NotEmpty();
        RuleFor(command => command.Item.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Item.Description).MaximumLength(3000);
        RuleFor(command => command.Item.OrderIndex).GreaterThanOrEqualTo(0);
        RuleFor(command => command.Item.StarRating).InclusiveBetween(1, 5)
            .When(command => command.Item.StarRating is not null);
        RuleFor(command => command.Item).Must(item =>
                (item.Latitude is null && item.Longitude is null) ||
                (item.Latitude is >= -90 and <= 90 && item.Longitude is >= -180 and <= 180))
            .WithMessage("Provide both a valid latitude and longitude, or leave location coordinates empty.");
    }
}

internal static class PackageOwnership
{
    public static async Task<(TravelAgency? Agency, TravelPackage? Package, Error? Error)> LoadAsync(
        Guid userId,
        Guid packageId,
        ITravelAgencyRepository agencies,
        ITravelPackageRepository packages,
        CancellationToken cancellationToken)
    {
        var agency = await agencies.GetByUserIdAsync(userId, cancellationToken: cancellationToken);
        if (agency is null) return (null, null, TravelAgencyErrors.AgencyAccountRequired);
        var package = await packages.GetByIdForAgencyAsync(packageId, agency.Id, includeDays: true, cancellationToken);
        if (package is not null) return (agency, package, null);
        return await packages.GetByIdAsync(packageId, cancellationToken: cancellationToken) is null
            ? (agency, null, TravelPackageErrors.NotFound)
            : (agency, null, TravelPackageErrors.OwnershipDenied);
    }

    public static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class SaveTravelPackageCityCommandHandler
    : IRequestHandler<SaveTravelPackageCityCommand, Result<TravelPackageResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;

    public SaveTravelPackageCityCommandHandler(ITravelAgencyRepository agencies, ITravelPackageRepository packages)
    {
        _agencies = agencies;
        _packages = packages;
    }

    public async Task<Result<TravelPackageResponse>> Handle(SaveTravelPackageCityCommand request, CancellationToken cancellationToken)
    {
        var loaded = await PackageOwnership.LoadAsync(request.UserId, request.PackageId, _agencies, _packages, cancellationToken);
        if (loaded.Error is not null) return Result<TravelPackageResponse>.Failure(loaded.Error);
        var package = loaded.Package!;
        var city = request.CityId is null
            ? new TravelPackageCity { Id = Guid.NewGuid(), TravelPackageId = package.Id, TravelPackage = package }
            : package.Cities.SingleOrDefault(value => value.Id == request.CityId);
        if (city is null) return Result<TravelPackageResponse>.Failure(TravelPackageErrors.CityNotFound);
        if (request.CityId is null)
        {
            city.OrderIndex = package.Cities.Count == 0 ? 0 : package.Cities.Max(value => value.OrderIndex) + 1;
            package.Cities.Add(city);
        }

        city.Name = request.Name.Trim();
        city.Nights = request.Nights;
        city.Description = PackageOwnership.Trim(request.Description);
        if (TravelPackageRoutePlanner.TotalNights(package) > package.DurationDays)
            return Result<TravelPackageResponse>.Failure(TravelPackageErrors.InvalidNightAllocation);
        if (request.CityId is null)
            await _packages.AddCityAsync(city, cancellationToken);
        TravelPackageRoutePlanner.AssignCitiesToDays(package);
        package.UpdatedAt = DateTime.UtcNow;
        await _packages.SaveChangesAsync(cancellationToken);
        return Result<TravelPackageResponse>.Success(TravelPackageMapping.ToResponse(package));
    }
}

public sealed class DeleteTravelPackageCityCommandHandler
    : IRequestHandler<DeleteTravelPackageCityCommand, Result<TravelPackageResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;

    public DeleteTravelPackageCityCommandHandler(ITravelAgencyRepository agencies, ITravelPackageRepository packages)
    {
        _agencies = agencies;
        _packages = packages;
    }

    public async Task<Result<TravelPackageResponse>> Handle(DeleteTravelPackageCityCommand request, CancellationToken cancellationToken)
    {
        var loaded = await PackageOwnership.LoadAsync(request.UserId, request.PackageId, _agencies, _packages, cancellationToken);
        if (loaded.Error is not null) return Result<TravelPackageResponse>.Failure(loaded.Error);
        var package = loaded.Package!;
        var city = package.Cities.SingleOrDefault(value => value.Id == request.CityId);
        if (city is null) return Result<TravelPackageResponse>.Failure(TravelPackageErrors.CityNotFound);
        foreach (var day in package.Days.Where(day => day.TravelPackageCityId == city.Id))
        {
            day.TravelPackageCityId = null;
            day.TravelPackageCity = null;
        }
        _packages.RemoveCity(city);
        package.Cities.Remove(city);
        var remaining = package.Cities.OrderBy(value => value.OrderIndex).ToArray();
        for (var index = 0; index < remaining.Length; index++) remaining[index].OrderIndex = index;
        TravelPackageRoutePlanner.AssignCitiesToDays(package);
        package.UpdatedAt = DateTime.UtcNow;
        await _packages.SaveChangesAsync(cancellationToken);
        return Result<TravelPackageResponse>.Success(TravelPackageMapping.ToResponse(package));
    }
}

public sealed class SaveTravelPackageItemCommandHandler
    : IRequestHandler<SaveTravelPackageItemCommand, Result<TravelPackageResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;

    public SaveTravelPackageItemCommandHandler(ITravelAgencyRepository agencies, ITravelPackageRepository packages)
    {
        _agencies = agencies;
        _packages = packages;
    }

    public async Task<Result<TravelPackageResponse>> Handle(SaveTravelPackageItemCommand request, CancellationToken cancellationToken)
    {
        var loaded = await PackageOwnership.LoadAsync(request.UserId, request.PackageId, _agencies, _packages, cancellationToken);
        if (loaded.Error is not null) return Result<TravelPackageResponse>.Failure(loaded.Error);
        var package = loaded.Package!;
        var day = package.Days.SingleOrDefault(value => value.Id == request.DayId);
        if (day is null) return Result<TravelPackageResponse>.Failure(TravelPackageErrors.DayNotFound);
        var item = request.ItemId is null
            ? new TravelPackageItem { Id = Guid.NewGuid(), TravelPackageDayId = day.Id, TravelPackageDay = day }
            : day.Items.SingleOrDefault(value => value.Id == request.ItemId);
        if (item is null) return Result<TravelPackageResponse>.Failure(TravelPackageErrors.ItemNotFound);
        if (request.ItemId is null) day.Items.Add(item);
        Apply(item, request.Item);
        if (request.ItemId is null)
            await _packages.AddItemAsync(item, cancellationToken);
        package.UpdatedAt = DateTime.UtcNow;
        await _packages.SaveChangesAsync(cancellationToken);
        return Result<TravelPackageResponse>.Success(TravelPackageMapping.ToResponse(package));
    }

    private static void Apply(TravelPackageItem item, TravelPackageItemInput input)
    {
        item.Type = input.Type;
        item.Source = input.Source;
        item.OrderIndex = input.OrderIndex;
        item.Title = input.Title.Trim();
        item.Description = PackageOwnership.Trim(input.Description);
        item.LocationName = PackageOwnership.Trim(input.LocationName);
        item.Address = PackageOwnership.Trim(input.Address);
        item.Latitude = input.Latitude;
        item.Longitude = input.Longitude;
        item.MapUrl = PackageOwnership.Trim(input.MapUrl);
        item.Provider = input.Source == TravelPackageItemSource.Provider ? PackageOwnership.Trim(input.Provider) : null;
        item.ProviderPlaceId = input.Source == TravelPackageItemSource.Provider ? PackageOwnership.Trim(input.ProviderPlaceId) : null;
        item.ProviderPhotoUrl = input.Source == TravelPackageItemSource.Provider ? PackageOwnership.Trim(input.ProviderPhotoUrl) : null;
        item.WebsiteUrl = input.Source == TravelPackageItemSource.Provider ? PackageOwnership.Trim(input.WebsiteUrl) : null;
        item.Cuisine = PackageOwnership.Trim(input.Cuisine);
        item.MealType = PackageOwnership.Trim(input.MealType);
        item.ReservationRequired = input.ReservationRequired;
        item.AccommodationType = PackageOwnership.Trim(input.AccommodationType);
        item.StarRating = input.StarRating;
        item.RoomType = PackageOwnership.Trim(input.RoomType);
        item.BoardBasis = PackageOwnership.Trim(input.BoardBasis);
        item.TransportMode = PackageOwnership.Trim(input.TransportMode);
        item.FromLocation = PackageOwnership.Trim(input.FromLocation);
        item.ToLocation = PackageOwnership.Trim(input.ToLocation);
        item.MeetingPoint = PackageOwnership.Trim(input.MeetingPoint);
        item.DifficultyLevel = PackageOwnership.Trim(input.DifficultyLevel);
        item.AdditionalDetails = PackageOwnership.Trim(input.AdditionalDetails);
    }
}

public sealed class DeleteTravelPackageItemCommandHandler
    : IRequestHandler<DeleteTravelPackageItemCommand, Result<TravelPackageResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;
    private readonly ITravelPackageImageStorage _storage;

    public DeleteTravelPackageItemCommandHandler(ITravelAgencyRepository agencies, ITravelPackageRepository packages, ITravelPackageImageStorage storage)
    {
        _agencies = agencies;
        _packages = packages;
        _storage = storage;
    }

    public async Task<Result<TravelPackageResponse>> Handle(DeleteTravelPackageItemCommand request, CancellationToken cancellationToken)
    {
        var loaded = await PackageOwnership.LoadAsync(request.UserId, request.PackageId, _agencies, _packages, cancellationToken);
        if (loaded.Error is not null) return Result<TravelPackageResponse>.Failure(loaded.Error);
        var package = loaded.Package!;
        var item = package.Days.SingleOrDefault(day => day.Id == request.DayId)?.Items.SingleOrDefault(value => value.Id == request.ItemId);
        if (item is null) return Result<TravelPackageResponse>.Failure(TravelPackageErrors.ItemNotFound);
        var urls = item.Images.Select(image => image.Url).ToArray();
        foreach (var image in item.Images.ToArray()) _packages.RemoveImage(image);
        _packages.RemoveItem(item);
        package.UpdatedAt = DateTime.UtcNow;
        await _packages.SaveChangesAsync(cancellationToken);
        foreach (var url in urls) await _storage.DeleteAsync(url, cancellationToken);
        return Result<TravelPackageResponse>.Success(TravelPackageMapping.ToResponse(package));
    }
}

public sealed class SearchPackagePlacesQueryHandler
    : IRequestHandler<SearchPackagePlacesQuery, Result<IReadOnlyList<PackagePlaceSearchResult>>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;
    private readonly IItineraryTravelDataService _travelData;

    public SearchPackagePlacesQueryHandler(ITravelAgencyRepository agencies, ITravelPackageRepository packages,
        IItineraryTravelDataService travelData)
    {
        _agencies = agencies;
        _packages = packages;
        _travelData = travelData;
    }

    public async Task<Result<IReadOnlyList<PackagePlaceSearchResult>>> Handle(SearchPackagePlacesQuery request, CancellationToken cancellationToken)
    {
        var loaded = await PackageOwnership.LoadAsync(request.UserId, request.PackageId, _agencies, _packages, cancellationToken);
        if (loaded.Error is not null) return Result<IReadOnlyList<PackagePlaceSearchResult>>.Failure(loaded.Error);
        var package = loaded.Package!;
        var day = package.Days.SingleOrDefault(value => value.Id == request.DayId);
        if (day is null) return Result<IReadOnlyList<PackagePlaceSearchResult>>.Failure(TravelPackageErrors.DayNotFound);
        // Always derive the city from route order and nights. The persisted relation exists
        // for querying only and must never override the package's current route definition.
        var city = TravelPackageRoutePlanner.ResolveCity(package, day.DayNumber);
        if (city is null) return Result<IReadOnlyList<PackagePlaceSearchResult>>.Failure(TravelPackageErrors.RouteRequired);
        var query = string.IsNullOrWhiteSpace(request.Query) ? DefaultQuery(request.Type) : request.Query.Trim();
        var places = request.Type == TravelPackageItemType.Accommodation
            ? await _travelData.SearchHotelsAsync(city.Name, query, package.StartDate, package.EndDate,
                1, package.CurrencyCode, "en", cancellationToken)
            : await _travelData.SearchLocalAsync($"{query} in {city.Name}", city.Name, "en", cancellationToken);

        IReadOnlyList<PackagePlaceSearchResult> response = places.Select(place => new PackagePlaceSearchResult(
            place.DataSource,
            place.PlaceId,
            place.Name,
            place.Address,
            PackageOwnership.Trim(place.Description),
            place.Rating,
            place.UserRatingsTotal,
            place.Latitude,
            place.Longitude,
            PackageOwnership.Trim(place.GoogleMapsLink),
            place.PhotoUrl,
            place.ExternalUrl)).ToArray();
        return Result<IReadOnlyList<PackagePlaceSearchResult>>.Success(response);
    }

    private static string DefaultQuery(TravelPackageItemType type) => type switch
    {
        TravelPackageItemType.Restaurant => "popular restaurants",
        TravelPackageItemType.Accommodation => "recommended hotels",
        TravelPackageItemType.Activity => "popular activities",
        _ => "top attractions"
    };
}

public sealed class UploadTravelPackageGalleryImageCommandHandler
    : IRequestHandler<UploadTravelPackageGalleryImageCommand, Result<TravelPackageResponse>>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;
    private readonly ITravelPackageImageStorage _storage;

    public UploadTravelPackageGalleryImageCommandHandler(ITravelAgencyRepository agencies, ITravelPackageRepository packages, ITravelPackageImageStorage storage)
    {
        _agencies = agencies;
        _packages = packages;
        _storage = storage;
    }

    public async Task<Result<TravelPackageResponse>> Handle(UploadTravelPackageGalleryImageCommand request, CancellationToken cancellationToken)
    {
        if (request.FileSize <= 0 || request.FileSize > 5 * 1024 * 1024 || !AllowedContentTypes.Contains(request.ContentType))
            return Result<TravelPackageResponse>.Failure(TravelPackageErrors.InvalidPackageImage);
        var loaded = await PackageOwnership.LoadAsync(request.UserId, request.PackageId, _agencies, _packages, cancellationToken);
        if (loaded.Error is not null) return Result<TravelPackageResponse>.Failure(loaded.Error);
        var package = loaded.Package!;
        var day = request.DayId is null ? null : package.Days.SingleOrDefault(value => value.Id == request.DayId);
        if (request.DayId is not null && day is null) return Result<TravelPackageResponse>.Failure(TravelPackageErrors.DayNotFound);
        var item = request.ItemId is null
            ? null
            : request.DayId is not null
                ? day?.Items.SingleOrDefault(value => value.Id == request.ItemId)
                : package.Days.SelectMany(value => value.Items).SingleOrDefault(value => value.Id == request.ItemId);
        if (request.ItemId is not null && item is null) return Result<TravelPackageResponse>.Failure(TravelPackageErrors.ItemNotFound);

        var extension = Path.GetExtension(request.FileName);
        if (string.IsNullOrWhiteSpace(extension)) extension = request.ContentType switch { "image/png" => ".png", "image/webp" => ".webp", _ => ".jpg" };
        string url;
        await using (var stream = new MemoryStream(request.FileContent, writable: false))
            url = await _storage.SaveAsync(stream, extension, cancellationToken);

        var targetImages = item?.Images ?? day?.Images ?? package.Images;
        var isPackageImage = item is null && day is null;
        var isCover = isPackageImage && (request.IsCover || !package.Images.Any(image => image.TravelPackageDayId == null && image.TravelPackageItemId == null));
        if (isCover)
            foreach (var existing in package.Images.Where(image => image.TravelPackageDayId == null && image.TravelPackageItemId == null)) existing.IsCover = false;
        var image = new TravelPackageImage
        {
            Id = Guid.NewGuid(),
            TravelPackageId = package.Id,
            TravelPackage = package,
            TravelPackageDayId = item is null ? day?.Id : null,
            TravelPackageDay = item is null ? day : null,
            TravelPackageItemId = item?.Id,
            TravelPackageItem = item,
            Url = url,
            Caption = PackageOwnership.Trim(request.Caption),
            AltText = PackageOwnership.Trim(request.AltText),
            SortOrder = targetImages.Count,
            IsCover = isCover,
            CreatedAt = DateTime.UtcNow
        };
        // Register the application-assigned image key explicitly as a new row. Relying only
        // on navigation discovery can make EF treat it as an existing entity and issue an
        // UPDATE that affects zero rows, which surfaces as DbUpdateConcurrencyException.
        await _packages.AddImageAsync(image, cancellationToken);
        if (isCover) package.CoverImageUrl = url;
        package.UpdatedAt = DateTime.UtcNow;
        try
        {
            await _packages.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _storage.DeleteAsync(url, cancellationToken);
            throw;
        }
        return Result<TravelPackageResponse>.Success(TravelPackageMapping.ToResponse(package));
    }
}

public sealed class DeleteTravelPackageGalleryImageCommandHandler
    : IRequestHandler<DeleteTravelPackageGalleryImageCommand, Result<TravelPackageResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;
    private readonly ITravelPackageImageStorage _storage;

    public DeleteTravelPackageGalleryImageCommandHandler(ITravelAgencyRepository agencies, ITravelPackageRepository packages, ITravelPackageImageStorage storage)
    {
        _agencies = agencies;
        _packages = packages;
        _storage = storage;
    }

    public async Task<Result<TravelPackageResponse>> Handle(DeleteTravelPackageGalleryImageCommand request, CancellationToken cancellationToken)
    {
        var loaded = await PackageOwnership.LoadAsync(request.UserId, request.PackageId, _agencies, _packages, cancellationToken);
        if (loaded.Error is not null) return Result<TravelPackageResponse>.Failure(loaded.Error);
        var package = loaded.Package!;
        var image = package.Images.SingleOrDefault(value => value.Id == request.ImageId)
            ?? package.Days.SelectMany(day => day.Images).SingleOrDefault(value => value.Id == request.ImageId)
            ?? package.Days.SelectMany(day => day.Items).SelectMany(item => item.Images).SingleOrDefault(value => value.Id == request.ImageId);
        if (image is null) return Result<TravelPackageResponse>.Failure(TravelPackageErrors.ImageNotFound);
        var wasCover = image.IsCover;
        var url = image.Url;
        _packages.RemoveImage(image);
        if (wasCover)
        {
            var next = package.Images.Where(value => value.Id != image.Id && value.TravelPackageDayId == null && value.TravelPackageItemId == null)
                .OrderBy(value => value.SortOrder).FirstOrDefault();
            if (next is not null) next.IsCover = true;
            package.CoverImageUrl = next?.Url;
        }
        package.UpdatedAt = DateTime.UtcNow;
        await _packages.SaveChangesAsync(cancellationToken);
        await _storage.DeleteAsync(url, cancellationToken);
        return Result<TravelPackageResponse>.Success(TravelPackageMapping.ToResponse(package));
    }
}
