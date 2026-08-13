using FluentValidation;

namespace Safarni.Application.Features.Trips.PublishTrip
{
    public sealed class PublishTripCommandValidator : AbstractValidator<PublishTripCommand>
    {
        public PublishTripCommandValidator()
        {
            RuleFor(command => command.TripId)
                .NotEmpty();

            RuleFor(command => command.ConfirmedNoPersonalInfo)
                .Equal(true)
                .WithMessage("You must confirm that companion names and personal details are not exposed.");

            RuleFor(command => command.CostAmount)
                .NotNull()
                .When(command => command.CostShown)
                .WithMessage("Cost amount is required when cost is shown.");
        }
    }
}