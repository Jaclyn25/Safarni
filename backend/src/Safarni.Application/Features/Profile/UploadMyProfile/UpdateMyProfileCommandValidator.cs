using FluentValidation;
using Safarni.Application.Features.Profile.UpdateMyProfile;

namespace Safarni.Application.Features.Profile.UploadMyProfile;

public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.FullName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.PhoneNumber)
            .MaximumLength(20)
            .Matches("^[0-9+\\-() ]+$")
            .When(command => !string.IsNullOrWhiteSpace(command.PhoneNumber));

        RuleFor(command => command.DateOfBirth)
            .LessThanOrEqualTo(DateTime.UtcNow.Date)
            .When(command => command.DateOfBirth.HasValue)
            .WithMessage("Date of birth cannot be in the future.");

        RuleFor(command => command.Country)
            .MaximumLength(100);

        RuleFor(command => command.City)
            .MaximumLength(100);

        RuleFor(command => command.Bio)
            .MaximumLength(500);
    }
}
