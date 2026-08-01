using FluentValidation;

namespace Safarni.Application.Features.Trips.CreateTrip;

public sealed class CreateTripCommandValidator
    : AbstractValidator<CreateTripCommand>
{
    public CreateTripCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Trip name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Destination)
            .NotEmpty().WithMessage("Destination is required.")
            .MaximumLength(200);

        RuleFor(x => x.OriginCountry)
            .NotEmpty().WithMessage("Origin country is required.")
            .MaximumLength(100);

        RuleFor(x => x.OriginCountryCode)
            .Matches("^[A-Za-z]{2}$")
            .When(x => !string.IsNullOrWhiteSpace(x.OriginCountryCode))
            .WithMessage("Origin country code must be a two-letter ISO code.");

        RuleFor(x => x.OriginCity)
            .MaximumLength(100);

        RuleFor(x => x.OriginAirportCode)
            .Matches("^[A-Za-z]{3}$")
            .When(x => !string.IsNullOrWhiteSpace(x.OriginAirportCode))
            .WithMessage("Origin airport code must be a three-letter IATA code.");

        RuleFor(x => x.OriginAirportName)
            .MaximumLength(200);

        RuleFor(x => x.OriginCity)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.OriginAirportCode))
            .WithMessage("Origin city is required when an origin airport is selected.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be on or after the start date.");

        RuleFor(x => x.Cities)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .NotEmpty().WithMessage("Choose at least one destination city.")
            .Must((command, cities) =>
                cities.Count <= TripRouteSerializer.GetTripDayCount(
                    command.StartDate,
                    command.EndDate))
            .WithMessage("Choose more trip days before selecting more cities.")
            .Must(cities => cities
                .Select(city => city.Name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() == cities.Count)
            .WithMessage("Each destination city can only be selected once.")
            .Must((command, cities) =>
                cities.Sum(city => city.Days) ==
                TripRouteSerializer.GetTripDayCount(
                    command.StartDate,
                    command.EndDate))
            .WithMessage("City day allocations must equal the total trip days.");

        RuleForEach(x => x.Cities)
            .ChildRules(city =>
            {
                city.RuleFor(value => value.Name)
                    .NotEmpty()
                    .MaximumLength(100);

                city.RuleFor(value => value.Days)
                    .GreaterThanOrEqualTo(1);

                city.RuleFor(value => value.AirportCode)
                    .Matches("^[A-Za-z]{3}$")
                    .When(value =>
                        !string.IsNullOrWhiteSpace(value.AirportCode))
                    .WithMessage("City airport code must be a three-letter IATA code.");

                city.RuleFor(value => value.AirportName)
                    .MaximumLength(200);
            });
    }
}
