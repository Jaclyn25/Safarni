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

public sealed record TravelPackageDayResponse(
    Guid Id,
    int DayNumber,
    string Title,
    string Description,
    string? Activities);

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
    string? CoverImageUrl,
    decimal? DiscountPercentage,
    string? OfferLabel,
    bool IsPublished,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    TravelPackageAgencyResponse Agency,
    IReadOnlyList<TravelPackageDayResponse> Days);

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
    string? OfferLabel)
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
    string? OfferLabel)
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
            package.CoverImageUrl,
            package.DiscountPercentage,
            package.OfferLabel,
            package.IsPublished,
            package.IsActive,
            package.CreatedAt,
            package.UpdatedAt,
            new TravelPackageAgencyResponse(
                package.TravelAgency.Id,
                package.TravelAgency.CompanyName,
                package.TravelAgency.LogoUrl,
                package.TravelAgency.VerificationStatus == Domain.Enums.AgencyVerificationStatus.Verified),
            package.Days
                .OrderBy(day => day.DayNumber)
                .Select(day => new TravelPackageDayResponse(
                    day.Id,
                    day.DayNumber,
                    day.Title,
                    day.Description,
                    day.Activities))
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
        string? offerLabel)
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
        package.CoverImageUrl = NullIfWhiteSpace(coverImageUrl);
        package.DiscountPercentage = discountPercentage;
        package.OfferLabel = NullIfWhiteSpace(offerLabel);
    }

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
            request.CoverImageUrl, request.DiscountPercentage, request.OfferLabel);
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

        TravelPackageMapping.Apply(package, request.Title, request.Description, request.Destination, request.StartDate,
            request.EndDate, request.DurationDays, request.Price, request.CurrencyCode, request.AvailableSeats,
            request.CoverImageUrl, request.DiscountPercentage, request.OfferLabel);
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
        if (package is null || !package.IsPublished || !package.IsActive ||
            package.TravelAgency.VerificationStatus != Domain.Enums.AgencyVerificationStatus.Verified)
        {
            return Result<TravelPackageResponse>.Failure(TravelPackageErrors.NotFound);
        }
        return Result<TravelPackageResponse>.Success(TravelPackageMapping.ToResponse(package));
    }
}
