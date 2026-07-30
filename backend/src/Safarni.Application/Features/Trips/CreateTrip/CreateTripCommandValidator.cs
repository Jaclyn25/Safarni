using FluentValidation;

namespace Safarni.Application.Features.Trips.CreateTrip;

public sealed class CreateTripCommandValidator : AbstractValidator<CreateTripCommand>
{
    public CreateTripCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Trip name is required.")
            .MaximumLength(200).WithMessage("Trip name must not exceed 200 characters.");

        RuleFor(x => x.Destination)
            .NotEmpty().WithMessage("Destination is required.")
            .MaximumLength(200).WithMessage("Destination must not exceed 200 characters.");

        RuleFor(x => x.OriginCountry)
            .NotEmpty().WithMessage("Origin country is required.")
            .MaximumLength(100);

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date must be greater than or equal to start date.");

        RuleFor(x => x.Cities)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Choose at least one destination city.")
            .NotEmpty()
            .WithMessage("Choose at least one destination city.")
            .Must((command, cities) =>
                cities.Count <=
                TripRouteSerializer.GetTripDayCount(
                    command.StartDate,
                    command.EndDate))
            .WithMessage(
                "Choose more days in your trip to be able to choose more cities.")
            .Must(cities =>
                cities
                    .Select(city => city.Name.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() == cities.Count)
            .WithMessage("Each destination city can only be selected once.")
            .Must((command, cities) =>
                cities.Sum(city => city.Days) ==
                TripRouteSerializer.GetTripDayCount(
                    command.StartDate,
                    command.EndDate))
            .WithMessage(
                "City day allocations must equal the total trip days.");

        RuleForEach(x => x.Cities)
            .ChildRules(city =>
            {
                city.RuleFor(value => value.Name)
                    .NotEmpty()
                    .MaximumLength(100);
                city.RuleFor(value => value.Days)
                    .GreaterThanOrEqualTo(1);
            });
    }
}
