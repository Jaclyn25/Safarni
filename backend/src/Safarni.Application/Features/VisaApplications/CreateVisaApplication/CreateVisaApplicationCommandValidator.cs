using FluentValidation;

namespace Safarni.Application.Features.VisaApplications.CreateVisaApplication;

public sealed class CreateVisaApplicationCommandValidator : AbstractValidator<CreateVisaApplicationCommand>
{
    public CreateVisaApplicationCommandValidator()
    {
        RuleFor(command => command.DestinationCountry)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.TravelPurpose)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.PlannedTravelDate)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date));
    }
}
