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

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date must be greater than or equal to start date.");
    }
}
