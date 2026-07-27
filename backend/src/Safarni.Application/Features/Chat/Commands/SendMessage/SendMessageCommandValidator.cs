using FluentValidation;

namespace ChatBotApi.Application.Features.Chat.Commands.SendMessage;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty().WithMessage("معرّف المحادثة غير صالح.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("معرّف المستخدم مطلوب.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("لا يمكن إرسال رسالة فارغة.")
            .MaximumLength(4000).WithMessage("الرسالة طويلة جداً، الحد الأقصى هو 4000 حرف.");
    }
}
