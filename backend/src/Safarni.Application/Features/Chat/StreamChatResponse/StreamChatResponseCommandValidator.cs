using FluentValidation;

namespace ChatBotApi.Application.Features.Chat.StreamChatResponse;

public sealed class StreamChatResponseCommandValidator
    : AbstractValidator<StreamChatResponseCommand>
{
    public StreamChatResponseCommandValidator()
    {
        RuleFor(command => command.ConversationId)
            .NotEmpty();

        RuleFor(command => command.UserId)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Message)
            .NotEmpty()
            .MaximumLength(4000);
    }
}
