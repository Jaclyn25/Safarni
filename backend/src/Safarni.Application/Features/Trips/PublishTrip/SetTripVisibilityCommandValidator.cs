using FluentValidation;

namespace Safarni.Application.Features.Trips.PublishTrip
{
    public sealed class SetTripVisibilityCommandValidator
        : AbstractValidator<SetTripVisibilityCommand>
    {
        public SetTripVisibilityCommandValidator()
        {
            RuleFor(command => command.TripId)
                .NotEmpty();

            RuleFor(command => command.Visibility)
                .IsInEnum();
        }
    }
}