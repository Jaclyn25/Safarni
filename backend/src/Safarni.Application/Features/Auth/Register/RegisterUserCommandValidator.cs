using FluentValidation;

namespace Safarni.Application.Features.Auth.Register;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.FullName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(8);

        RuleFor(command => command.ConfirmPassword)
            .Equal(command => command.Password)
            .WithMessage("Password confirmation does not match.");

        RuleFor(command => command.AccountType)
            .IsInEnum();

        RuleFor(command => command.PhoneNumber)
            .MaximumLength(20);

        When(
            command => command.AccountType ==
                Safarni.Domain.Enums.RegistrationAccountType.TravelAgency,
            () =>
            {
                RuleFor(command => command.PhoneNumber)
                    .NotEmpty();

                RuleFor(command => command.Agency)
                    .NotNull()
                    .SetValidator(new TravelAgencyRegistrationDetailsValidator()!);
            });
    }
}

public sealed class TravelAgencyRegistrationDetailsValidator
    : AbstractValidator<TravelAgencyRegistrationDetails>
{
    public TravelAgencyRegistrationDetailsValidator()
    {
        RuleFor(details => details.CompanyName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(details => details.Description)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(details => details.Address)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(details => details.City)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(details => details.Country)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(details => details.CompanyPhone)
            .NotEmpty()
            .MaximumLength(30);

        RuleFor(details => details.LogoUrl)
            .MaximumLength(500)
            .Must(BeHttpUrl)
            .When(details => !string.IsNullOrWhiteSpace(details.LogoUrl))
            .WithMessage("Logo URL must be an absolute HTTP or HTTPS URL.");

        RuleFor(details => details.WebsiteUrl)
            .MaximumLength(500)
            .Must(BeHttpUrl)
            .When(details => !string.IsNullOrWhiteSpace(details.WebsiteUrl))
            .WithMessage("Website URL must be an absolute HTTP or HTTPS URL.");
    }

    private static bool BeHttpUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp ||
             uri.Scheme == Uri.UriSchemeHttps);
    }
}
