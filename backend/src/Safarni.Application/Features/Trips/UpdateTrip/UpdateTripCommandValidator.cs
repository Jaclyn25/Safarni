using FluentValidation;

namespace Safarni.Application.Features.Trips.UpdateTrip;

public sealed class UpdateTripCommandValidator
    : AbstractValidator<UpdateTripCommand>
{
    public UpdateTripCommandValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Destination)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.OriginCountry)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.OriginCountryCode)
            .Matches("^[A-Za-z]{2}$")
            .When(x => !string.IsNullOrWhiteSpace(x.OriginCountryCode));

        RuleFor(x => x.OriginCity)
            .MaximumLength(100);

        RuleFor(x => x.OriginAirportCode)
            .Matches("^[A-Za-z]{3}$")
            .When(x => !string.IsNullOrWhiteSpace(x.OriginAirportCode));

        RuleFor(x => x.OriginAirportName)
            .MaximumLength(200);

        RuleFor(x => x.OriginCity)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.OriginAirportCode))
            .WithMessage("Origin city is required when an origin airport is selected.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate);

        RuleFor(x => x.Cities)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .NotEmpty()
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
                        !string.IsNullOrWhiteSpace(value.AirportCode));
                city.RuleFor(value => value.AirportName)
                    .MaximumLength(200);
            });
    }
}
