using ChatBotApi.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Safarni.Domain.Entities;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Application.UnitTests.Chat;

public sealed class ChatPersistenceContractTests
{
    [Fact]
    public void ChatModel_EnforcesUserOwnershipAndFolderOwnershipBoundary()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=SafarniModelTests;Trusted_Connection=True;")
            .Options;
        using var context = new ApplicationDbContext(options);

        var conversation = context.Model.FindEntityType(typeof(Conversation));
        var folder = context.Model.FindEntityType(typeof(ChatFolder));
        var message = context.Model.FindEntityType(typeof(ConversationMessage));

        conversation.Should().NotBeNull();
        folder.Should().NotBeNull();
        message.Should().NotBeNull();

        conversation!.FindProperty(nameof(Conversation.UserId))!.ClrType
            .Should().Be<Guid>();
        folder!.FindProperty(nameof(ChatFolder.UserId))!.ClrType
            .Should().Be<Guid>();

        var conversationOwner = conversation.GetForeignKeys()
            .Single(key => key.PrincipalEntityType.ClrType == typeof(User));
        conversationOwner.Properties.Select(property => property.Name)
            .Should().Equal(nameof(Conversation.UserId));
        conversationOwner.IsRequired.Should().BeTrue();
        conversationOwner.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);

        var folderOwner = folder.GetForeignKeys()
            .Single(key => key.PrincipalEntityType.ClrType == typeof(User));
        folderOwner.Properties.Select(property => property.Name)
            .Should().Equal(nameof(ChatFolder.UserId));
        folderOwner.IsRequired.Should().BeTrue();
        folderOwner.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);

        var folderRelationship = conversation.GetForeignKeys()
            .Single(key => key.PrincipalEntityType.ClrType == typeof(ChatFolder));
        folderRelationship.Properties.Select(property => property.Name)
            .Should().Equal(nameof(Conversation.FolderId), nameof(Conversation.UserId));
        folderRelationship.PrincipalKey.Properties.Select(property => property.Name)
            .Should().Equal(nameof(ChatFolder.Id), nameof(ChatFolder.UserId));
        folderRelationship.DeleteBehavior.Should().Be(DeleteBehavior.ClientSetNull);

        conversation.GetIndexes().Should().Contain(index => index.Properties
            .Select(property => property.Name)
            .SequenceEqual(new[]
            {
                nameof(Conversation.UserId), nameof(Conversation.CreatedAt)
            }));
        message!.GetIndexes().Should().Contain(index => index.Properties
            .Select(property => property.Name)
            .SequenceEqual(new[]
            {
                nameof(ConversationMessage.ConversationId),
                nameof(ConversationMessage.CreatedAt)
            }));
    }
}
