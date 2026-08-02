using ChatBotApi.Domain.Entities;
using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Auditing;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Features.Admin.Common;
using Safarni.Application.Features.Admin.Knowledge;
using Safarni.Domain.Enums;

namespace Safarni.Application.UnitTests.Admin.Knowledge;

public class KnowledgeCommandsTests
{
    private static readonly Guid ActorId = Guid.NewGuid();

    private static Mock<ICurrentUserService> CreateCurrentUser()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UserId).Returns(ActorId);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        return currentUser;
    }

    private static Mock<IAuditLogWriter> CreateAuditWriter()
    {
        var writer = new Mock<IAuditLogWriter>();
        writer
            .Setup(x => x.WriteAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<AuditSeverity>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return writer;
    }

    private static TravelKnowledge CreateKnowledge(int id = 1)
    {
        return new TravelKnowledge
        {
            Id = id,
            Title = "Schengen visa",
            Category = "VisaInfo",
            Content = "Content here",
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task Create_WhenTitleMissing_ShouldFail()
    {
        var adminRepository = new Mock<IAdminRepository>();
        var handler = new CreateKnowledgeCommandHandler(
            adminRepository.Object,
            CreateAuditWriter().Object,
            CreateCurrentUser().Object);

        var result = await handler.Handle(
            new CreateKnowledgeCommand(
                new KnowledgeInput(
                    Title: "  ",
                    Category: "General",
                    Content: "valid content")),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AdminErrors.KnowledgeValidationFailed.Code);
        adminRepository.Verify(
            x => x.AddKnowledgeAsync(
                It.IsAny<TravelKnowledge>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_WhenContentMissing_ShouldFail()
    {
        var handler = new CreateKnowledgeCommandHandler(
            new Mock<IAdminRepository>().Object,
            CreateAuditWriter().Object,
            CreateCurrentUser().Object);

        var result = await handler.Handle(
            new CreateKnowledgeCommand(
                new KnowledgeInput(
                    Title: "Valid title",
                    Category: "General",
                    Content: null!)),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AdminErrors.KnowledgeValidationFailed.Code);
    }

    [Fact]
    public async Task Create_WhenValid_ShouldPersistTrimmedItemAndAudit()
    {
        TravelKnowledge? addedItem = null;
        var adminRepository = new Mock<IAdminRepository>();
        adminRepository
            .Setup(x => x.AddKnowledgeAsync(
                It.IsAny<TravelKnowledge>(),
                It.IsAny<CancellationToken>()))
            .Callback<TravelKnowledge, CancellationToken>((item, _) =>
            {
                addedItem = item;
            })
            .Returns(Task.CompletedTask);

        var auditWriter = CreateAuditWriter();

        var handler = new CreateKnowledgeCommandHandler(
            adminRepository.Object,
            auditWriter.Object,
            CreateCurrentUser().Object);

        var result = await handler.Handle(
            new CreateKnowledgeCommand(
                new KnowledgeInput(
                    Title: "  Visa guide  ",
                    Category: "VisaInfo",
                    Content: "  Content  ",
                    Country: "France")),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        addedItem.Should().NotBeNull();
        addedItem!.Title.Should().Be("Visa guide");
        addedItem.Content.Should().Be("Content");
        addedItem.Country.Should().Be("France");
        addedItem.UpdatedByUserId.Should().Be(ActorId);
        result.Value!.Title.Should().Be("Visa guide");

        auditWriter.Verify(
            x => x.WriteAsync(
                ActorId,
                AdminAuditActions.KnowledgeCreated,
                "Knowledge",
                It.IsAny<string>(),
                It.IsAny<object?>(),
                It.IsAny<AuditSeverity>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Update_WhenItemMissing_ShouldFail()
    {
        var adminRepository = new Mock<IAdminRepository>();
        adminRepository
            .Setup(x => x.GetKnowledgeByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TravelKnowledge?)null);

        var handler = new UpdateKnowledgeCommandHandler(
            adminRepository.Object,
            CreateAuditWriter().Object,
            CreateCurrentUser().Object);

        var result = await handler.Handle(
            new UpdateKnowledgeCommand(
                42,
                new KnowledgeInput(
                    Title: "Title",
                    Category: "General",
                    Content: "Content")),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AdminErrors.KnowledgeNotFound.Code);
    }

    [Fact]
    public async Task Update_WhenValid_ShouldApplyChangesAndAudit()
    {
        var item = CreateKnowledge();
        var adminRepository = new Mock<IAdminRepository>();
        adminRepository
            .Setup(x => x.GetKnowledgeByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var auditWriter = CreateAuditWriter();

        var handler = new UpdateKnowledgeCommandHandler(
            adminRepository.Object,
            auditWriter.Object,
            CreateCurrentUser().Object);

        var result = await handler.Handle(
            new UpdateKnowledgeCommand(
                1,
                new KnowledgeInput(
                    Title: "Updated title",
                    Category: "",
                    Content: "Updated content",
                    City: "Paris")),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.Title.Should().Be("Updated title");
        item.Category.Should().Be("VisaInfo");
        item.Content.Should().Be("Updated content");
        item.City.Should().Be("Paris");
        item.UpdatedByUserId.Should().Be(ActorId);
        adminRepository.Verify(
            x => x.SaveKnowledgeChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        auditWriter.Verify(
            x => x.WriteAsync(
                ActorId,
                AdminAuditActions.KnowledgeUpdated,
                "Knowledge",
                "1",
                It.IsAny<object?>(),
                It.IsAny<AuditSeverity>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Archive_WhenItemMissing_ShouldFail()
    {
        var adminRepository = new Mock<IAdminRepository>();
        adminRepository
            .Setup(x => x.GetKnowledgeByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TravelKnowledge?)null);

        var handler = new ArchiveKnowledgeCommandHandler(
            adminRepository.Object,
            CreateAuditWriter().Object,
            CreateCurrentUser().Object);

        var result = await handler.Handle(
            new ArchiveKnowledgeCommand(1, Archived: true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AdminErrors.KnowledgeNotFound.Code);
    }

    [Fact]
    public async Task Archive_WhenArchiving_ShouldSetFlagAndAuditArchive()
    {
        var item = CreateKnowledge();
        var adminRepository = new Mock<IAdminRepository>();
        adminRepository
            .Setup(x => x.GetKnowledgeByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var auditWriter = CreateAuditWriter();

        var handler = new ArchiveKnowledgeCommandHandler(
            adminRepository.Object,
            auditWriter.Object,
            CreateCurrentUser().Object);

        var result = await handler.Handle(
            new ArchiveKnowledgeCommand(1, Archived: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.IsArchived.Should().BeTrue();

        auditWriter.Verify(
            x => x.WriteAsync(
                ActorId,
                AdminAuditActions.KnowledgeArchived,
                "Knowledge",
                "1",
                It.IsAny<object?>(),
                It.IsAny<AuditSeverity>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Restore_WhenRestoring_ShouldClearFlagAndAuditRestore()
    {
        var item = CreateKnowledge();
        item.IsArchived = true;
        var adminRepository = new Mock<IAdminRepository>();
        adminRepository
            .Setup(x => x.GetKnowledgeByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var auditWriter = CreateAuditWriter();

        var handler = new ArchiveKnowledgeCommandHandler(
            adminRepository.Object,
            auditWriter.Object,
            CreateCurrentUser().Object);

        var result = await handler.Handle(
            new ArchiveKnowledgeCommand(1, Archived: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.IsArchived.Should().BeFalse();

        auditWriter.Verify(
            x => x.WriteAsync(
                ActorId,
                AdminAuditActions.KnowledgeRestored,
                "Knowledge",
                "1",
                It.IsAny<object?>(),
                It.IsAny<AuditSeverity>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
