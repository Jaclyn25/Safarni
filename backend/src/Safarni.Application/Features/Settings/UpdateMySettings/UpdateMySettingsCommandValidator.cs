using FluentValidation;

namespace Safarni.Application.Features.Settings.UpdateMySettings;

public sealed class UpdateMySettingsCommandValidator : AbstractValidator<UpdateMySettingsCommand>
{
    public UpdateMySettingsCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();
    }
}
