using FluentValidation;

namespace Safarni.Application.Features.Preferences.UpsertTravelProfile;

public sealed class UpsertTravelProfileCommandValidator : AbstractValidator<UpsertTravelProfileCommand>
{
    public UpsertTravelProfileCommandValidator()
    {
        RuleFor(command => command.PreferredDestinationCountry)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.TravelPurpose)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.TravelStyle)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.MinBudget)
            .GreaterThanOrEqualTo(0)
            .When(command => command.MinBudget.HasValue);

        RuleFor(command => command.MaxBudget)
            .GreaterThanOrEqualTo(0)
            .When(command => command.MaxBudget.HasValue);

        RuleFor(command => command)
            .Must(command => !command.MinBudget.HasValue || !command.MaxBudget.HasValue || command.MinBudget <= command.MaxBudget)
            .WithMessage("MinBudget must be less than or equal to MaxBudget.");

        RuleFor(command => command.PassportNumber)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(command => command.Nationality)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.PassportExpiryDate)
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow.Date));
    }
}
