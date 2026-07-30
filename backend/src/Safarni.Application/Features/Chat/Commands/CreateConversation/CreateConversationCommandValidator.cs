using FluentValidation;

namespace ChatBotApi.Application.Features.Chat.Commands.CreateConversation;

public class CreateConversationCommandValidator : AbstractValidator<CreateConversationCommand>
{
    public CreateConversationCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("معرّف المستخدم مطلوب.");

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("عنوان المحادثة طويل جداً، الحد الأقصى هو 200 حرف.");
    }
}
