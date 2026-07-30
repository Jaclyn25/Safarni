using FluentValidation;
using Safarni.Application.Features.Trips.GenerateItinerary;

namespace Safarni.Application.Features.Trips.Itineraries.ManageItems;

public sealed class AddItineraryItemCommandValidator
    : AbstractValidator<AddItineraryItemCommand>
{
    public AddItineraryItemCommandValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.DayId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1500);
        RuleFor(x => x.Location).MaximumLength(300);
        RuleFor(x => x.Category).Must(ItineraryItemInput.IsValidCategory);
        RuleFor(x => x.StartTime).Must(ItineraryItemInput.IsValidTime);
        RuleFor(x => x.EndTime).Must(ItineraryItemInput.IsValidTime);
        RuleFor(x => x.EstimatedCost).GreaterThanOrEqualTo(0).When(x => x.EstimatedCost.HasValue);
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0).When(x => x.OrderIndex.HasValue);
    }
}

public sealed class UpdateItineraryItemCommandValidator
    : AbstractValidator<UpdateItineraryItemCommand>
{
    public UpdateItineraryItemCommandValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.TargetDayId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1500);
        RuleFor(x => x.Location).MaximumLength(300);
        RuleFor(x => x.Category).Must(ItineraryItemInput.IsValidCategory);
        RuleFor(x => x.StartTime).Must(ItineraryItemInput.IsValidTime);
        RuleFor(x => x.EndTime).Must(ItineraryItemInput.IsValidTime);
        RuleFor(x => x.EstimatedCost).GreaterThanOrEqualTo(0).When(x => x.EstimatedCost.HasValue);
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
    }
}

public sealed class SelectItineraryVenueCommandValidator
    : AbstractValidator<SelectItineraryVenueCommand>
{
    public SelectItineraryVenueCommandValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.PlaceId)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(300);
        RuleFor(x => x.Address)
            .MaximumLength(500);
        RuleFor(x => x.PhotoUrl)
            .MaximumLength(1000);
        RuleFor(x => x.GoogleMapsUrl)
            .MaximumLength(1000);
        RuleFor(x => x.ExternalUrl)
            .MaximumLength(1500);
        RuleFor(x => x.TotalRate)
            .GreaterThanOrEqualTo(0)
            .When(x => x.TotalRate.HasValue);
        RuleFor(x => x.RatePerNight)
            .GreaterThanOrEqualTo(0)
            .When(x => x.RatePerNight.HasValue);
    }
}
