using FluentValidation;
using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Packages;

public sealed record AddTravelPackageDayCommand(
    Guid UserId,
    Guid PackageId,
    int DayNumber,
    string Title,
    string Description,
    string? Activities)
    : IRequest<Result<TravelPackageResponse>>;

public sealed record UpdateTravelPackageDayCommand(
    Guid UserId,
    Guid PackageId,
    Guid DayId,
    int DayNumber,
    string Title,
    string Description,
    string? Activities)
    : IRequest<Result<TravelPackageResponse>>;

public sealed record DeleteTravelPackageDayCommand(
    Guid UserId,
    Guid PackageId,
    Guid DayId)
    : IRequest<Result<TravelPackageResponse>>;

public sealed class AddTravelPackageDayCommandValidator
    : AbstractValidator<AddTravelPackageDayCommand>
{
    public AddTravelPackageDayCommandValidator()
    {
        DayRules.Apply(this, command => command.DayNumber, command => command.Title,
            command => command.Description, command => command.Activities);
    }
}

public sealed class UpdateTravelPackageDayCommandValidator
    : AbstractValidator<UpdateTravelPackageDayCommand>
{
    public UpdateTravelPackageDayCommandValidator()
    {
        RuleFor(command => command.DayId).NotEmpty();
        DayRules.Apply(this, command => command.DayNumber, command => command.Title,
            command => command.Description, command => command.Activities);
    }
}

internal static class DayRules
{
    public static void Apply<T>(
        AbstractValidator<T> validator,
        Func<T, int> dayNumber,
        Func<T, string> title,
        Func<T, string> description,
        Func<T, string?> activities)
    {
        validator.RuleFor(command => dayNumber(command)).GreaterThan(0);
        validator.RuleFor(command => title(command)).NotEmpty().MaximumLength(200);
        validator.RuleFor(command => description(command)).NotEmpty().MaximumLength(2000);
        validator.RuleFor(command => activities(command)).MaximumLength(4000);
    }
}

public sealed class AddTravelPackageDayCommandHandler
    : IRequestHandler<AddTravelPackageDayCommand, Result<TravelPackageResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;

    public AddTravelPackageDayCommandHandler(ITravelAgencyRepository agencies, ITravelPackageRepository packages)
    {
        _agencies = agencies;
        _packages = packages;
    }

    public async Task<Result<TravelPackageResponse>> Handle(AddTravelPackageDayCommand request, CancellationToken cancellationToken)
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
        if (!IsValidDayNumber(package, request.DayNumber))
            return Result<TravelPackageResponse>.Failure(TravelPackageErrors.InvalidDayNumber);
        if (package.Days.Any(day => day.DayNumber == request.DayNumber))
            return Result<TravelPackageResponse>.Failure(TravelPackageErrors.DuplicateDayNumber);

        package.Days.Add(new TravelPackageDay
        {
            TravelPackageId = package.Id,
            DayNumber = request.DayNumber,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Activities = NullIfWhiteSpace(request.Activities)
        });
        await _packages.SaveChangesAsync(cancellationToken);
        return Result<TravelPackageResponse>.Success(TravelPackageMapping.ToResponse(package));
    }

    internal static bool IsValidDayNumber(TravelPackage package, int dayNumber) =>
        dayNumber >= 1 && dayNumber <= package.DurationDays;

    internal static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class UpdateTravelPackageDayCommandHandler
    : IRequestHandler<UpdateTravelPackageDayCommand, Result<TravelPackageResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;

    public UpdateTravelPackageDayCommandHandler(ITravelAgencyRepository agencies, ITravelPackageRepository packages)
    {
        _agencies = agencies;
        _packages = packages;
    }

    public async Task<Result<TravelPackageResponse>> Handle(UpdateTravelPackageDayCommand request, CancellationToken cancellationToken)
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
        if (!AddTravelPackageDayCommandHandler.IsValidDayNumber(package, request.DayNumber))
            return Result<TravelPackageResponse>.Failure(TravelPackageErrors.InvalidDayNumber);
        var day = package.Days.SingleOrDefault(item => item.Id == request.DayId);
        if (day is null) return Result<TravelPackageResponse>.Failure(TravelPackageErrors.DayNotFound);
        if (package.Days.Any(item => item.Id != request.DayId && item.DayNumber == request.DayNumber))
            return Result<TravelPackageResponse>.Failure(TravelPackageErrors.DuplicateDayNumber);

        day.DayNumber = request.DayNumber;
        day.Title = request.Title.Trim();
        day.Description = request.Description.Trim();
        day.Activities = AddTravelPackageDayCommandHandler.NullIfWhiteSpace(request.Activities);
        await _packages.SaveChangesAsync(cancellationToken);
        return Result<TravelPackageResponse>.Success(TravelPackageMapping.ToResponse(package));
    }
}

public sealed class DeleteTravelPackageDayCommandHandler
    : IRequestHandler<DeleteTravelPackageDayCommand, Result<TravelPackageResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;

    public DeleteTravelPackageDayCommandHandler(ITravelAgencyRepository agencies, ITravelPackageRepository packages)
    {
        _agencies = agencies;
        _packages = packages;
    }

    public async Task<Result<TravelPackageResponse>> Handle(DeleteTravelPackageDayCommand request, CancellationToken cancellationToken)
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
        var day = package.Days.SingleOrDefault(item => item.Id == request.DayId);
        if (day is null) return Result<TravelPackageResponse>.Failure(TravelPackageErrors.DayNotFound);
        _packages.RemoveDay(day);
        await _packages.SaveChangesAsync(cancellationToken);
        return Result<TravelPackageResponse>.Success(TravelPackageMapping.ToResponse(package));
    }
}
