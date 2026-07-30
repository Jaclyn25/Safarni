using FluentValidation;

namespace ChatBotApi.Application.Features.Chat.Commands.RegenerateResponse;

public sealed class RegenerateResponseCommandValidator : AbstractValidator<RegenerateResponseCommand>
{
    public RegenerateResponseCommandValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty().WithMessage("معرّف المحادثة غير صالح.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("معرّف المستخدم مطلوب.");
    }
}
