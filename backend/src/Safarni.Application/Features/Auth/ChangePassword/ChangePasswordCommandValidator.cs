using FluentValidation;

namespace Safarni.Application.Features.Auth.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(6);

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty();
    }
}