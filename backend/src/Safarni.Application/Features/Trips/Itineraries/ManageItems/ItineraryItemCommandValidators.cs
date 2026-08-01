using FluentValidation;
using Safarni.Application.Features.Trips.GenerateItinerary;

namespace Safarni.Application.Features.Trips.Itineraries.ManageItems;

public sealed class AddItineraryItemCommandValidator
    : AbstractValidator<AddItineraryItemCommand>
{
    public AddItineraryItemCommandValidator()
    {
        RuleFor(command => command.TripId).NotEmpty();
        RuleFor(command => command.DayId).NotEmpty();
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(command => command.Description)
            .MaximumLength(1500);
        RuleFor(command => command.Location)
            .MaximumLength(300);
        RuleFor(command => command.Category)
            .Must(ItineraryItemInput.IsValidCategory)
            .WithMessage("Select a supported itinerary category.");
        RuleFor(command => command.StartTime)
            .Must(ItineraryItemInput.IsValidTime)
            .WithMessage("Start time must use HH:mm.");
        RuleFor(command => command.EndTime)
            .Must(ItineraryItemInput.IsValidTime)
            .WithMessage("End time must use HH:mm.");
        RuleFor(command => command.EstimatedCost)
            .GreaterThanOrEqualTo(0)
            .When(command => command.EstimatedCost.HasValue);
        RuleFor(command => command.OrderIndex)
            .GreaterThanOrEqualTo(0)
            .When(command => command.OrderIndex.HasValue);
        RuleFor(command => command)
            .Must(command => TimeRangeIsValid(
                command.StartTime,
                command.EndTime))
            .WithMessage("End time must be later than start time.");
    }

    internal static bool TimeRangeIsValid(
        string? startTime,
        string? endTime)
    {
        var start = ItineraryItemInput.ParseTime(startTime);
        var end = ItineraryItemInput.ParseTime(endTime);
        return start is null || end is null || end > start;
    }
}

public sealed class UpdateItineraryItemCommandValidator
    : AbstractValidator<UpdateItineraryItemCommand>
{
    public UpdateItineraryItemCommandValidator()
    {
        RuleFor(command => command.TripId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(command => command.Description)
            .MaximumLength(1500);
        RuleFor(command => command.Location)
            .MaximumLength(300);
        RuleFor(command => command.Category)
            .Must(ItineraryItemInput.IsValidCategory)
            .WithMessage("Select a supported itinerary category.");
        RuleFor(command => command.StartTime)
            .Must(ItineraryItemInput.IsValidTime)
            .WithMessage("Start time must use HH:mm.");
        RuleFor(command => command.EndTime)
            .Must(ItineraryItemInput.IsValidTime)
            .WithMessage("End time must use HH:mm.");
        RuleFor(command => command.EstimatedCost)
            .GreaterThanOrEqualTo(0)
            .When(command => command.EstimatedCost.HasValue);
        RuleFor(command => command)
            .Must(command => AddItineraryItemCommandValidator.TimeRangeIsValid(
                command.StartTime,
                command.EndTime))
            .WithMessage("End time must be later than start time.");
    }
}

public sealed class AddRecommendedItineraryItemCommandValidator
    : AbstractValidator<AddRecommendedItineraryItemCommand>
{
    public AddRecommendedItineraryItemCommandValidator()
    {
        RuleFor(command => command.TripId).NotEmpty();
        RuleFor(command => command.DayId).NotEmpty();
        RuleFor(command => command.Category)
            .Must(ItineraryItemInput.IsValidCategory)
            .WithMessage("Select a supported itinerary category.");
        RuleFor(command => command.PlaceId)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(command => command.OrderIndex)
            .GreaterThanOrEqualTo(0)
            .When(command => command.OrderIndex.HasValue);
    }
}

public sealed class MoveItineraryItemCommandValidator
    : AbstractValidator<MoveItineraryItemCommand>
{
    public MoveItineraryItemCommandValidator()
    {
        RuleFor(command => command.TripId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
        RuleFor(command => command.TargetDayId).NotEmpty();
        RuleFor(command => command.TargetOrderIndex)
            .GreaterThanOrEqualTo(0);
    }
}

public sealed class SelectItineraryVenueCommandValidator
    : AbstractValidator<SelectItineraryVenueCommand>
{
    public SelectItineraryVenueCommandValidator()
    {
        RuleFor(command => command.TripId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
        RuleFor(command => command.PlaceId)
            .NotEmpty()
            .MaximumLength(200);
    }
}
