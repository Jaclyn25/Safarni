using FluentValidation;

namespace Safarni.Application.Features.Trips.GenerateItinerary.Jobs;

public sealed class StartItineraryGenerationCommandValidator
    : AbstractValidator<StartItineraryGenerationCommand>
{
    public StartItineraryGenerationCommandValidator()
    {
        RuleFor(command => command.TripId).NotEmpty();
        RuleFor(command => command.Preferences)
            .NotNull()
            .SetValidator(new GenerateItineraryPreferencesValidator());
        RuleFor(command => command.BudgetAmount)
            .GreaterThan(0)
            .LessThanOrEqualTo(10_000_000)
            .When(command => command.BudgetAmount.HasValue);
        RuleFor(command => command.CurrencyCode)
            .NotEmpty()
            .Matches("^[A-Za-z]{3}$")
            .WithMessage(
                "Currency code must contain exactly three letters.");
        RuleFor(command => command.TravelerCount)
            .InclusiveBetween(1, 20);
        RuleFor(command => command.IdempotencyKey)
            .MaximumLength(100);
    }
}
