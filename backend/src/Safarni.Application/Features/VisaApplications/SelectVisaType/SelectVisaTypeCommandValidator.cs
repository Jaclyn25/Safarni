using FluentValidation;

namespace Safarni.Application.Features.VisaApplications.SelectVisaType;

public sealed class SelectVisaTypeCommandValidator : AbstractValidator<SelectVisaTypeCommand>
{
    public SelectVisaTypeCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.VisaType)
            .NotEmpty()
            .MaximumLength(100);
    }
}
