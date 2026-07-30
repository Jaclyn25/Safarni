using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Features.Chat.Commands.DeleteConversation;
using ChatBotApi.Application.Features.Chat.Queries.GetConversationById;
using ChatBotApi.Application.Features.Chat.Queries.GetConversations;
using ChatBotApi.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Safarni.Application.UnitTests.Chat;

public class ChatConversationCrudHandlerTests
{
    [Fact]
    public async Task GetConversationById_ShouldReturnMessagesInChronologicalOrder()
    {
        var repository = new Mock<IChatRepository>();
        var conversation = ConversationWithMessages();
        conversation.Messages.Reverse();
        repository
            .Setup(x => x.GetByIdAsync(
                conversation.Id,
                conversation.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var result = await new GetConversationByIdQueryHandler(repository.Object).Handle(
            new GetConversationByIdQuery(conversation.Id, conversation.UserId),
            CancellationToken.None);

        result.Messages.Select(message => message.Content)
            .Should().Equal("first", "second");
    }

    [Fact]
    public async Task GetConversationById_ShouldNotReturnAnotherUsersConversation()
    {
        var repository = new Mock<IChatRepository>();
        var conversationId = Guid.NewGuid();
        repository
            .Setup(x => x.GetByIdAsync(
                conversationId,
                "intruder",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var action = () => new GetConversationByIdQueryHandler(repository.Object).Handle(
            new GetConversationByIdQuery(conversationId, "intruder"),
            CancellationToken.None);

        await action.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetConversations_ShouldHideIncompleteChatsAndOrderNewestFirst()
    {
        var repository = new Mock<IChatRepository>();
        var older = ConversationWithMessages(DateTime.UtcNow.AddDays(-2));
        var newer = ConversationWithMessages(DateTime.UtcNow.AddHours(-1));
        var incomplete = ConversationWithMessages(DateTime.UtcNow);
        incomplete.Messages.RemoveAt(1);
        repository
            .Setup(x => x.GetByUserIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([older, incomplete, newer]);

        var result = await new GetConversationsQueryHandler(repository.Object).Handle(
            new GetConversationsQuery("user-1"),
            CancellationToken.None);

        result.Select(conversation => conversation.Id)
            .Should().Equal(newer.Id, older.Id);
    }

    [Fact]
    public async Task DeleteConversation_ShouldDeleteOwnedConversationAndSave()
    {
        var repository = new Mock<IChatRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var conversation = ConversationWithMessages();
        repository
            .Setup(x => x.GetByIdAsync(
                conversation.Id,
                conversation.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await new DeleteConversationCommandHandler(
            repository.Object,
            unitOfWork.Object).Handle(
                new DeleteConversationCommand(conversation.Id, conversation.UserId),
                CancellationToken.None);

        result.Should().BeTrue();
        repository.Verify(x => x.Delete(conversation), Times.Once);
        unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Conversation ConversationWithMessages(DateTime? createdAt = null)
    {
        var start = createdAt ?? DateTime.UtcNow.AddMinutes(-2);
        return new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Title = "Trip",
            CreatedAt = start,
            Messages =
            [
                new ConversationMessage
                {
                    Id = Guid.NewGuid(),
                    Role = "user",
                    Content = "first",
                    CreatedAt = start.AddMinutes(1)
                },
                new ConversationMessage
                {
                    Id = Guid.NewGuid(),
                    Role = "assistant",
                    Content = "second",
                    CreatedAt = start.AddMinutes(2)
                }
            ]
        };
    }
}
