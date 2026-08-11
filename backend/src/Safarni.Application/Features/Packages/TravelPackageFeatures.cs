using FluentValidation;
using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Abstractions.Security;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Packages;

public sealed record TravelPackageAgencyResponse(
    Guid AgencyId,
    string CompanyName,
    string? LogoUrl,
    bool IsVerified);

public sealed record TravelPackageImageResponse(
    Guid Id,
    string Url,
    string? Caption,
    string? AltText,
    int SortOrder,
    bool IsCover);

public sealed record TravelPackageItemResponse(
    Guid Id,
    string Type,
    string Source,
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
    string? AdditionalDetails,
    IReadOnlyList<TravelPackageImageResponse> Images);

public sealed record TravelPackageCityResponse(
    Guid Id,
    string Name,
    int OrderIndex,
    int Nights,
    string? Description);

public sealed record TravelPackageDayResponse(
    Guid Id,
    int DayNumber,
    string Title,
    string Description,
    Guid? CityId,
    string? CityName,
    IReadOnlyList<TravelPackageItemResponse> Items,
    IReadOnlyList<TravelPackageImageResponse> Images);

public sealed record TravelPackageResponse(
    Guid Id,
    Guid TravelAgencyId,
    string Title,
    string Description,
    string Destination,
    DateTime StartDate,
    DateTime EndDate,
    int DurationDays,
    decimal Price,
    string CurrencyCode,
    int AvailableSeats,
    int RemainingSeats,
    string? CoverImageUrl,
    decimal? DiscountPercentage,
    string? OfferLabel,
    string? Inclusions,
    string? Exclusions,
    string? ImportantNotes,
    string? CancellationPolicy,
    bool IsPublished,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    TravelPackageAgencyResponse Agency,
    IReadOnlyList<TravelPackageCityResponse> Cities,
    IReadOnlyList<TravelPackageDayResponse> Days,
    IReadOnlyList<TravelPackageImageResponse> Images);

public sealed record CreateTravelPackageCommand(
    Guid UserId,
    string Title,
    string Description,
    string Destination,
    DateTime StartDate,
    DateTime EndDate,
    int DurationDays,
    decimal Price,
    string CurrencyCode,
    int AvailableSeats,
    string? CoverImageUrl,
    decimal? DiscountPercentage,
    string? OfferLabel,
    string? Inclusions = null,
    string? Exclusions = null,
    string? ImportantNotes = null,
    string? CancellationPolicy = null)
    : IRequest<Result<TravelPackageResponse>>;

public sealed record UpdateTravelPackageCommand(
    Guid UserId,
    Guid PackageId,
    string Title,
    string Description,
    string Destination,
    DateTime StartDate,
    DateTime EndDate,
    int DurationDays,
    decimal Price,
    string CurrencyCode,
    int AvailableSeats,
    string? CoverImageUrl,
    decimal? DiscountPercentage,
    string? OfferLabel,
    string? Inclusions = null,
    string? Exclusions = null,
    string? ImportantNotes = null,
    string? CancellationPolicy = null)
    : IRequest<Result<TravelPackageResponse>>;

public sealed record ListAgencyPackagesQuery(Guid UserId)
    : IRequest<Result<IReadOnlyList<TravelPackageResponse>>>;

public sealed record GetAgencyPackageQuery(Guid UserId, Guid PackageId)
    : IRequest<Result<TravelPackageResponse>>;

public sealed record PublishTravelPackageCommand(Guid UserId, Guid PackageId)
    : IRequest<Result<TravelPackageResponse>>;

public sealed record UnpublishTravelPackageCommand(Guid UserId, Guid PackageId)
    : IRequest<Result<TravelPackageResponse>>;

public sealed record DeactivateTravelPackageCommand(Guid UserId, Guid PackageId)
    : IRequest<Result<TravelPackageResponse>>;

public sealed record ListPublishedPackagesQuery(
    string? Destination,
    DateTime? StartDate,
    int? DurationDays,
    decimal? MinPrice,
    decimal? MaxPrice,
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<PagedResult<TravelPackageResponse>>>;

public sealed record GetPublishedPackageQuery(Guid PackageId)
    : IRequest<Result<TravelPackageResponse>>;

public sealed class CreateTravelPackageCommandValidator
    : AbstractValidator<CreateTravelPackageCommand>
{
    public CreateTravelPackageCommandValidator()
    {
        TravelPackageValidationRules.Apply(this, command => new TravelPackageCommandShape(
            command.Title, command.Description, command.Destination, command.StartDate, command.EndDate,
            command.DurationDays, command.Price, command.CurrencyCode, command.AvailableSeats,
            command.CoverImageUrl, command.DiscountPercentage, command.OfferLabel));
        RuleFor(command => command.Inclusions).MaximumLength(4000);
        RuleFor(command => command.Exclusions).MaximumLength(4000);
        RuleFor(command => command.ImportantNotes).MaximumLength(4000);
        RuleFor(command => command.CancellationPolicy).MaximumLength(4000);
    }
}

public sealed class UpdateTravelPackageCommandValidator
    : AbstractValidator<UpdateTravelPackageCommand>
{
    public UpdateTravelPackageCommandValidator()
    {
        TravelPackageValidationRules.Apply(this, command => new TravelPackageCommandShape(
            command.Title, command.Description, command.Destination, command.StartDate, command.EndDate,
            command.DurationDays, command.Price, command.CurrencyCode, command.AvailableSeats,
            command.CoverImageUrl, command.DiscountPercentage, command.OfferLabel));
        RuleFor(command => command.Inclusions).MaximumLength(4000);
        RuleFor(command => command.Exclusions).MaximumLength(4000);
        RuleFor(command => command.ImportantNotes).MaximumLength(4000);
        RuleFor(command => command.CancellationPolicy).MaximumLength(4000);
        RuleFor(command => command.PackageId).NotEmpty();
    }
}

internal static class TravelPackageValidationRules
{
    public static void Apply<T>(
        AbstractValidator<T> validator,
        Func<T, TravelPackageCommandShape> shape)
    {
        validator.RuleFor(command => shape(command).Title)
            .NotEmpty().WithMessage("Package title is required.")
            .MaximumLength(200);
        validator.RuleFor(command => shape(command).Description)
            .NotEmpty().WithMessage("Package description is required.")
            .MaximumLength(4000);
        validator.RuleFor(command => shape(command).Destination)
            .NotEmpty().WithMessage("Package destination is required.")
            .MaximumLength(200);
        validator.RuleFor(command => shape(command).EndDate)
            .GreaterThanOrEqualTo(command => shape(command).StartDate)
            .WithMessage("Package end date must be on or after the start date.");
        validator.RuleFor(command => shape(command).DurationDays)
            .GreaterThan(0)
            .Must((command, duration) => duration ==
                (shape(command).EndDate.Date - shape(command).StartDate.Date).Days + 1)
            .WithMessage("Duration must match the inclusive package date range.");
        validator.RuleFor(command => shape(command).Price)
            .GreaterThanOrEqualTo(0);
        validator.RuleFor(command => shape(command).CurrencyCode)
            .NotEmpty()
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency code must be a three-letter ISO code.");
        validator.RuleFor(command => shape(command).AvailableSeats)
            .GreaterThan(0)
            .LessThanOrEqualTo(100000);
        validator.RuleFor(command => shape(command).CoverImageUrl)
            .MaximumLength(1000);
        validator.RuleFor(command => shape(command).DiscountPercentage)
            .InclusiveBetween(0, 100)
            .When(command => shape(command).DiscountPercentage is not null);
        validator.RuleFor(command => shape(command).OfferLabel)
            .MaximumLength(200);
    }
}

internal sealed record TravelPackageCommandShape(
    string Title,
    string Description,
    string Destination,
    DateTime StartDate,
    DateTime EndDate,
    int DurationDays,
    decimal Price,
    string CurrencyCode,
    int AvailableSeats,
    string? CoverImageUrl,
    decimal? DiscountPercentage,
    string? OfferLabel);

internal static class TravelPackageMapping
{
    public static TravelPackageResponse ToResponse(TravelPackage package)
    {
        var packageImages = package.Images
            .Where(image => image.TravelPackageDayId == null && image.TravelPackageItemId == null)
            .OrderByDescending(image => image.IsCover)
            .ThenBy(image => image.SortOrder)
            .ToArray();
        var canonicalCoverUrl = packageImages.FirstOrDefault(image => image.IsCover)?.Url
            ?? package.CoverImageUrl;

        return new TravelPackageResponse(
            package.Id,
            package.TravelAgencyId,
            package.Title,
            package.Description,
            package.Destination,
            package.StartDate,
            package.EndDate,
            package.DurationDays,
            package.Price,
            package.CurrencyCode,
            package.AvailableSeats,
            Math.Max(0, package.AvailableSeats - package.BookingRequests
                .Where(request => request.Status == Domain.Enums.PackageBookingRequestStatus.Accepted)
                .Sum(request => request.TravelerCount)),
            canonicalCoverUrl,
            package.DiscountPercentage,
            package.OfferLabel,
            package.Inclusions,
            package.Exclusions,
            package.ImportantNotes,
            package.CancellationPolicy,
            package.IsPublished,
            package.IsActive,
            package.CreatedAt,
            package.UpdatedAt,
            new TravelPackageAgencyResponse(
                package.TravelAgency.Id,
                package.TravelAgency.CompanyName,
                package.TravelAgency.LogoUrl,
                package.TravelAgency.VerificationStatus == Domain.Enums.AgencyVerificationStatus.Verified),
            package.Cities
                .OrderBy(city => city.OrderIndex)
                .Select(city => new TravelPackageCityResponse(
                    city.Id, city.Name, city.OrderIndex, city.Nights, city.Description))
                .ToArray(),
            package.Days
                .OrderBy(day => day.DayNumber)
                .Select(day =>
                {
                    var routeCity = TravelPackageRoutePlanner.ResolveCity(package, day.DayNumber);
                    return new TravelPackageDayResponse(
                        day.Id,
                        day.DayNumber,
                        day.Title,
                        day.Description,
                        routeCity?.Id,
                        routeCity?.Name,
                        day.Items.OrderBy(item => item.OrderIndex).Select(ToItemResponse).ToArray(),
                        day.Images.OrderBy(image => image.SortOrder).Select(ToImageResponse).ToArray());
                })
                .ToArray(),
            packageImages
                .Select(ToImageResponse)
                .ToArray());
    }

    public static void Apply(
        TravelPackage package,
        string title,
        string description,
        string destination,
        DateTime startDate,
        DateTime endDate,
        int durationDays,
        decimal price,
        string currencyCode,
        int availableSeats,
        string? coverImageUrl,
        decimal? discountPercentage,
        string? offerLabel,
        string? inclusions = null,
        string? exclusions = null,
        string? importantNotes = null,
        string? cancellationPolicy = null)
    {
        package.Title = title.Trim();
        package.Description = description.Trim();
        package.Destination = destination.Trim();
        package.StartDate = startDate.Date;
        package.EndDate = endDate.Date;
        package.DurationDays = durationDays;
        package.Price = price;
        package.CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        package.AvailableSeats = availableSeats;
        // An uploaded gallery cover is authoritative. This prevents an older URL retained
        // in an open edit form from overwriting a newly selected cover on the next save.
        package.CoverImageUrl = package.Images
            .FirstOrDefault(image => image.TravelPackageDayId == null &&
                image.TravelPackageItemId == null && image.IsCover)?.Url
            ?? NullIfWhiteSpace(coverImageUrl);
        package.DiscountPercentage = discountPercentage;
        package.OfferLabel = NullIfWhiteSpace(offerLabel);
        package.Inclusions = NullIfWhiteSpace(inclusions);
        package.Exclusions = NullIfWhiteSpace(exclusions);
        package.ImportantNotes = NullIfWhiteSpace(importantNotes);
        package.CancellationPolicy = NullIfWhiteSpace(cancellationPolicy);
    }

    internal static TravelPackageImageResponse ToImageResponse(TravelPackageImage image) =>
        new(image.Id, image.Url, image.Caption, image.AltText, image.SortOrder, image.IsCover);

    internal static TravelPackageItemResponse ToItemResponse(TravelPackageItem item) =>
        new(
            item.Id, item.Type.ToString(), item.Source.ToString(), item.OrderIndex, item.Title,
            item.Description, item.LocationName,
            item.Address, item.Latitude, item.Longitude, item.MapUrl, item.Provider, item.ProviderPlaceId,
            item.ProviderPhotoUrl, item.WebsiteUrl, item.Cuisine,
            item.MealType, item.ReservationRequired, item.AccommodationType, item.StarRating,
            item.RoomType, item.BoardBasis, item.TransportMode, item.FromLocation, item.ToLocation,
            item.MeetingPoint, item.DifficultyLevel, item.AdditionalDetails,
            item.Images.OrderBy(image => image.SortOrder).Select(ToImageResponse).ToArray());

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class CreateTravelPackageCommandHandler
    : IRequestHandler<CreateTravelPackageCommand, Result<TravelPackageResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;
    private readonly IEntitlementService? _entitlements;

    public CreateTravelPackageCommandHandler(ITravelAgencyRepository agencies, ITravelPackageRepository packages, IEntitlementService? entitlements = null)
    {
        _agencies = agencies;
        _packages = packages;
        _entitlements = entitlements;
    }

    public async Task<Result<TravelPackageResponse>> Handle(CreateTravelPackageCommand request, CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(request.UserId, cancellationToken: cancellationToken);
        if (agency is null)
        {
            return Result<TravelPackageResponse>.Failure(TravelAgencyErrors.AgencyAccountRequired);
        }
        if (_entitlements is not null &&
            !(await _entitlements.GetAgencyPackageCreationUsageAsync(agency.Id, cancellationToken)).IsAllowed)
        {
            return Result<TravelPackageResponse>.Failure(BillingErrors.AgencyPackageCreationLimitReached);
        }

        var package = new TravelPackage
        {
            Id = Guid.NewGuid(),
            TravelAgencyId = agency.Id,
            TravelAgency = agency,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        TravelPackageMapping.Apply(package, request.Title, request.Description, request.Destination, request.StartDate,
            request.EndDate, request.DurationDays, request.Price, request.CurrencyCode, request.AvailableSeats,
            request.CoverImageUrl, request.DiscountPercentage, request.OfferLabel, request.Inclusions,
            request.Exclusions, request.ImportantNotes, request.CancellationPolicy);
        await _packages.AddAsync(package, cancellationToken);
        await _packages.SaveChangesAsync(cancellationToken);
        return Result<TravelPackageResponse>.Success(TravelPackageMapping.ToResponse(package));
    }
}

public sealed class UpdateTravelPackageCommandHandler
    : IRequestHandler<UpdateTravelPackageCommand, Result<TravelPackageResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;

    public UpdateTravelPackageCommandHandler(ITravelAgencyRepository agencies, ITravelPackageRepository packages)
    {
        _agencies = agencies;
        _packages = packages;
    }

    public async Task<Result<TravelPackageResponse>> Handle(UpdateTravelPackageCommand request, CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(request.UserId, cancellationToken: cancellationToken);
        if (agency is null)
        {
            return Result<TravelPackageResponse>.Failure(TravelAgencyErrors.AgencyAccountRequired);
        }

        var package = await _packages.GetByIdForAgencyAsync(request.PackageId, agency.Id, includeDays: true, cancellationToken: cancellationToken);
        if (package is null)
        {
            return await OwnershipOrNotFound(request.PackageId, cancellationToken);
        }

        if (package.Days.Any(day => day.DayNumber > request.DurationDays))
            return Result<TravelPackageResponse>.Failure(TravelPackageErrors.InvalidDayNumber);
        if (TravelPackageRoutePlanner.TotalNights(package) > request.DurationDays)
            return Result<TravelPackageResponse>.Failure(TravelPackageErrors.InvalidNightAllocation);

        TravelPackageMapping.Apply(package, request.Title, request.Description, request.Destination, request.StartDate,
            request.EndDate, request.DurationDays, request.Price, request.CurrencyCode, request.AvailableSeats,
            request.CoverImageUrl, request.DiscountPercentage, request.OfferLabel, request.Inclusions,
            request.Exclusions, request.ImportantNotes, request.CancellationPolicy);
        TravelPackageRoutePlanner.AssignCitiesToDays(package);
        package.UpdatedAt = DateTime.UtcNow;
        await _packages.SaveChangesAsync(cancellationToken);
        return Result<TravelPackageResponse>.Success(TravelPackageMapping.ToResponse(package));
    }

    private async Task<Result<TravelPackageResponse>> OwnershipOrNotFound(Guid packageId, CancellationToken cancellationToken)
    {
        return await _packages.GetByIdAsync(packageId, cancellationToken: cancellationToken) is null
            ? Result<TravelPackageResponse>.Failure(TravelPackageErrors.NotFound)
            : Result<TravelPackageResponse>.Failure(TravelPackageErrors.OwnershipDenied);
    }
}

public sealed class ListAgencyPackagesQueryHandler
    : IRequestHandler<ListAgencyPackagesQuery, Result<IReadOnlyList<TravelPackageResponse>>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;

    public ListAgencyPackagesQueryHandler(ITravelAgencyRepository agencies, ITravelPackageRepository packages)
    {
        _agencies = agencies;
        _packages = packages;
    }

    public async Task<Result<IReadOnlyList<TravelPackageResponse>>> Handle(ListAgencyPackagesQuery request, CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(request.UserId, cancellationToken: cancellationToken);
        if (agency is null)
        {
            return Result<IReadOnlyList<TravelPackageResponse>>.Failure(TravelAgencyErrors.AgencyAccountRequired);
        }
        var packages = await _packages.ListByAgencyAsync(agency.Id, cancellationToken);
        return Result<IReadOnlyList<TravelPackageResponse>>.Success(packages.Select(TravelPackageMapping.ToResponse).ToArray());
    }
}

public sealed class GetAgencyPackageQueryHandler
    : IRequestHandler<GetAgencyPackageQuery, Result<TravelPackageResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;

    public GetAgencyPackageQueryHandler(ITravelAgencyRepository agencies, ITravelPackageRepository packages)
    {
        _agencies = agencies;
        _packages = packages;
    }

    public async Task<Result<TravelPackageResponse>> Handle(GetAgencyPackageQuery request, CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(request.UserId, cancellationToken: cancellationToken);
        if (agency is null)
        {
            return Result<TravelPackageResponse>.Failure(TravelAgencyErrors.AgencyAccountRequired);
        }
        var package = await _packages.GetByIdForAgencyAsync(request.PackageId, agency.Id, includeDays: true, cancellationToken: cancellationToken);
        if (package is not null)
        {
            return Result<TravelPackageResponse>.Success(TravelPackageMapping.ToResponse(package));
        }
        return await _packages.GetByIdAsync(request.PackageId, cancellationToken: cancellationToken) is null
            ? Result<TravelPackageResponse>.Failure(TravelPackageErrors.NotFound)
            : Result<TravelPackageResponse>.Failure(TravelPackageErrors.OwnershipDenied);
    }
}

public sealed class PublishTravelPackageCommandHandler
    : IRequestHandler<PublishTravelPackageCommand, Result<TravelPackageResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;
    private readonly ITravelAgencyAuthorizationService _authorization;
    private readonly IEntitlementService? _entitlements;

    public PublishTravelPackageCommandHandler(ITravelAgencyRepository agencies, ITravelPackageRepository packages, ITravelAgencyAuthorizationService authorization, IEntitlementService? entitlements = null)
    {
        _agencies = agencies;
        _packages = packages;
        _authorization = authorization;
        _entitlements = entitlements;
    }

    public async Task<Result<TravelPackageResponse>> Handle(PublishTravelPackageCommand request, CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(request.UserId, cancellationToken: cancellationToken);
        if (agency is null)
        {
            return Result<TravelPackageResponse>.Failure(TravelAgencyErrors.AgencyAccountRequired);
        }
        if (!await _authorization.IsVerifiedAsync(request.UserId, cancellationToken))
        {
            return Result<TravelPackageResponse>.Failure(TravelPackageErrors.PublishingRequiresVerifiedAgency);
        }
        if (_entitlements is not null && !(await _entitlements.GetAgencyPackageUsageAsync(agency.Id, cancellationToken)).IsAllowed)
        {
            return Result<TravelPackageResponse>.Failure(BillingErrors.AgencyPackageLimitReached);
        }
        var package = await _packages.GetByIdForAgencyAsync(request.PackageId, agency.Id, includeDays: true, cancellationToken: cancellationToken);
        if (package is null)
        {
            return await _packages.GetByIdAsync(request.PackageId, cancellationToken: cancellationToken) is null
                ? Result<TravelPackageResponse>.Failure(TravelPackageErrors.NotFound)
                : Result<TravelPackageResponse>.Failure(TravelPackageErrors.OwnershipDenied);
        }
        if (!package.IsActive) return Result<TravelPackageResponse>.Failure(TravelPackageErrors.AlreadyInactive);
        if (package.IsPublished) return Result<TravelPackageResponse>.Failure(TravelPackageErrors.AlreadyPublished);
        if (!TravelPackageRoutePlanner.HasValidNightAllocation(package))
        {
            return Result<TravelPackageResponse>.Failure(TravelPackageErrors.InvalidNightAllocation);
        }

        // Route nights are the single source of truth. Repair legacy/manual assignments
        // before validating and publishing so old package data cannot leak into the program.
        TravelPackageRoutePlanner.AssignCitiesToDays(package);
        var cityIds = package.Cities.Select(city => city.Id).ToHashSet();
        var expectedDayNumbers = Enumerable.Range(1, package.DurationDays).ToHashSet();
        var actualDayNumbers = package.Days.Select(day => day.DayNumber).ToArray();
        if (package.DurationDays <= 0 ||
            package.Days.Count != package.DurationDays ||
            actualDayNumbers.Distinct().Count() != package.DurationDays ||
            !expectedDayNumbers.SetEquals(actualDayNumbers) ||
            package.Days.Any(day => day.TravelPackageCityId is null || !cityIds.Contains(day.TravelPackageCityId.Value)))
        {
            return Result<TravelPackageResponse>.Failure(TravelPackageErrors.IncompleteProgram);
        }
        var now = DateTime.UtcNow;
        package.IsPublished = true;
        package.PublishedAt = now;
        package.UpdatedAt = now;
        await _packages.SaveChangesAsync(cancellationToken);
        return Result<TravelPackageResponse>.Success(TravelPackageMapping.ToResponse(package));
    }
}

public sealed class UnpublishTravelPackageCommandHandler
    : IRequestHandler<UnpublishTravelPackageCommand, Result<TravelPackageResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;

    public UnpublishTravelPackageCommandHandler(ITravelAgencyRepository agencies, ITravelPackageRepository packages)
    {
        _agencies = agencies;
        _packages = packages;
    }

    public async Task<Result<TravelPackageResponse>> Handle(UnpublishTravelPackageCommand request, CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(request.UserId, cancellationToken: cancellationToken);
        if (agency is null) return Result<TravelPackageResponse>.Failure(TravelAgencyErrors.AgencyAccountRequired);
        var package = await _packages.GetByIdForAgencyAsync(request.PackageId, agency.Id, includeDays: true, cancellationToken: cancellationToken);
        if (package is null)
        {
            return await _packages.GetByIdAsync(request.PackageId, cancellationToken: cancellationToken) is null
                ? Result<TravelPackageResponse>.Failure(TravelPackageErrors.NotFound)
                : Result<TravelPackageResponse>.Failure(TravelPackageErrors.OwnershipDenied);
        }
        if (!package.IsPublished) return Result<TravelPackageResponse>.Failure(TravelPackageErrors.AlreadyUnpublished);
        package.IsPublished = false;
        package.UpdatedAt = DateTime.UtcNow;
        await _packages.SaveChangesAsync(cancellationToken);
        return Result<TravelPackageResponse>.Success(TravelPackageMapping.ToResponse(package));
    }
}

public sealed class DeactivateTravelPackageCommandHandler
    : IRequestHandler<DeactivateTravelPackageCommand, Result<TravelPackageResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;

    public DeactivateTravelPackageCommandHandler(ITravelAgencyRepository agencies, ITravelPackageRepository packages)
    {
        _agencies = agencies;
        _packages = packages;
    }

    public async Task<Result<TravelPackageResponse>> Handle(DeactivateTravelPackageCommand request, CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(request.UserId, cancellationToken: cancellationToken);
        if (agency is null) return Result<TravelPackageResponse>.Failure(TravelAgencyErrors.AgencyAccountRequired);
        var package = await _packages.GetByIdForAgencyAsync(request.PackageId, agency.Id, includeDays: true, cancellationToken);
        if (package is null)
        {
            return await _packages.GetByIdAsync(request.PackageId, cancellationToken: cancellationToken) is null
                ? Result<TravelPackageResponse>.Failure(TravelPackageErrors.NotFound)
                : Result<TravelPackageResponse>.Failure(TravelPackageErrors.OwnershipDenied);
        }
        if (!package.IsActive) return Result<TravelPackageResponse>.Failure(TravelPackageErrors.AlreadyInactive);
        package.IsActive = false;
        package.IsPublished = false;
        package.UpdatedAt = DateTime.UtcNow;
        await _packages.SaveChangesAsync(cancellationToken);
        return Result<TravelPackageResponse>.Success(TravelPackageMapping.ToResponse(package));
    }
}

public sealed class ListPublishedPackagesQueryHandler
    : IRequestHandler<ListPublishedPackagesQuery, Result<PagedResult<TravelPackageResponse>>>
{
    private readonly ITravelPackageRepository _packages;

    public ListPublishedPackagesQueryHandler(ITravelPackageRepository packages)
    {
        _packages = packages;
    }

    public async Task<Result<PagedResult<TravelPackageResponse>>> Handle(ListPublishedPackagesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var result = await _packages.ListPublishedAsync(
            new PublishedPackageFilters(request.Destination, request.StartDate, request.DurationDays,
                request.MinPrice, request.MaxPrice, page, pageSize), cancellationToken);
        return Result<PagedResult<TravelPackageResponse>>.Success(
            new PagedResult<TravelPackageResponse>(result.Items.Select(TravelPackageMapping.ToResponse).ToArray(), result.Page, result.PageSize, result.TotalCount));
    }
}

public sealed class GetPublishedPackageQueryHandler
    : IRequestHandler<GetPublishedPackageQuery, Result<TravelPackageResponse>>
{
    private readonly ITravelPackageRepository _packages;

    public GetPublishedPackageQueryHandler(ITravelPackageRepository packages)
    {
        _packages = packages;
    }

    public async Task<Result<TravelPackageResponse>> Handle(GetPublishedPackageQuery request, CancellationToken cancellationToken)
    {
        var package = await _packages.GetByIdAsync(request.PackageId, includeDays: true, cancellationToken: cancellationToken);
        if (package is null || !package.IsPublished || !package.IsActive || package.EndDate < DateTime.UtcNow.Date ||
            package.TravelAgency.VerificationStatus != Domain.Enums.AgencyVerificationStatus.Verified)
        {
            return Result<TravelPackageResponse>.Failure(TravelPackageErrors.NotFound);
        }
        return Result<TravelPackageResponse>.Success(TravelPackageMapping.ToResponse(package));
    }
}
