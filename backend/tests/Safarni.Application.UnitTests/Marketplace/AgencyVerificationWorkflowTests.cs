using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Auditing;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Notifications;
using Safarni.Application.Abstractions.Storage;
using Safarni.Application.Common.Errors;
using Safarni.Application.Features.Admin.Agencies;
using Safarni.Application.Features.Agencies.Documents;
using Safarni.Application.Features.Agencies.Onboarding;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.UnitTests.Marketplace;

public sealed class AgencyVerificationWorkflowTests
{
    private readonly Mock<ITravelAgencyRepository> _agencies = new();
    private readonly Mock<IAgencyVerificationDocumentStorage> _storage = new();
    private readonly Mock<IAuditLogWriter> _auditLog = new();
    private readonly Mock<IMarketplaceNotificationService> _notifications = new();

    [Fact]
    public void UploadValidator_RejectsUnsupportedFilesOversizeFilesAndPastExpiry()
    {
        var validator = new UploadAgencyVerificationDocumentCommandValidator();
        var command = new UploadAgencyVerificationDocumentCommand(
            Guid.NewGuid(),
            AgencyVerificationDocumentType.TaxCard,
            "TAX-1",
            DateTime.UtcNow.Date.AddDays(-1),
            "tax.exe",
            "application/octet-stream",
            UploadAgencyVerificationDocumentCommandValidator.MaxFileSizeBytes + 1,
            [1, 2, 3]);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(command.ContentType));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(command.FileSize));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(command.ExpiryDate));
    }

    [Fact]
    public async Task Upload_RejectsFileWhoseSignatureDoesNotMatchDeclaredType()
    {
        var userId = Guid.NewGuid();
        _agencies.Setup(repository => repository.GetByUserIdAsync(
                userId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Agency(userId));
        var handler = new UploadAgencyVerificationDocumentCommandHandler(
            _agencies.Object,
            _storage.Object,
            _notifications.Object);

        var result = await handler.Handle(
            new UploadAgencyVerificationDocumentCommand(
                userId,
                AgencyVerificationDocumentType.CommercialRegistration,
                "CR-123",
                null,
                "company.pdf",
                "application/pdf",
                4,
                [1, 2, 3, 4]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TravelAgencyErrors.InvalidDocument);
        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DocumentContent_DeniesAnotherAgencyBeforeOpeningPrivateStorage()
    {
        var agency = Agency(Guid.NewGuid());
        _agencies.Setup(repository => repository.GetByIdAsync(
                agency.Id,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(agency);
        var handler = new GetAgencyVerificationDocumentContentQueryHandler(
            _agencies.Object,
            _storage.Object);

        var result = await handler.Handle(
            new GetAgencyVerificationDocumentContentQuery(
                Guid.NewGuid(),
                agency.Id,
                AgencyVerificationDocumentType.TaxCard,
                IsAuthorizedAdmin: false),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TravelAgencyErrors.DocumentAccessDenied);
        _agencies.Verify(repository => repository.GetDocumentAsync(
            It.IsAny<Guid>(),
            It.IsAny<AgencyVerificationDocumentType>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DocumentContent_AllowsAuthorizedAdminWithoutExposingStorageReference()
    {
        var agency = Agency(Guid.NewGuid());
        var document = Document(agency.Id, AgencyVerificationDocumentType.TaxCard);
        _agencies.Setup(repository => repository.GetByIdAsync(
                agency.Id,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(agency);
        _agencies.Setup(repository => repository.GetDocumentAsync(
                agency.Id,
                document.DocumentType,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);
        _storage.Setup(storage => storage.OpenReadAsync(
                document.StorageReference,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream("%PDF-test"u8.ToArray()));
        var handler = new GetAgencyVerificationDocumentContentQueryHandler(
            _agencies.Object,
            _storage.Object);

        var result = await handler.Handle(
            new GetAgencyVerificationDocumentContentQuery(
                Guid.NewGuid(),
                agency.Id,
                document.DocumentType,
                IsAuthorizedAdmin: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.FileName.Should().Be(document.OriginalFileName);
        result.Value.Content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Approve_FailsWhenAnyRequiredDocumentIsMissing()
    {
        var agency = Agency(Guid.NewGuid());
        agency.VerificationDocuments.Add(Document(
            agency.Id,
            AgencyVerificationDocumentType.CommercialRegistration));
        _agencies.Setup(repository => repository.GetByIdAsync(
                agency.Id,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(agency);
        var handler = new DecideAgencyVerificationCommandHandler(
            _agencies.Object,
            _auditLog.Object,
            _notifications.Object);

        var result = await handler.Handle(
            new DecideAgencyVerificationCommand(
                agency.Id,
                Guid.NewGuid(),
                AgencyVerificationDecision.Approve,
                null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TravelAgencyErrors.RequiredDocumentsMissing);
        _agencies.Verify(repository => repository.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Approve_WithCompleteDocuments_StoresReviewerTimestampAndAudit()
    {
        var agency = Agency(Guid.NewGuid());
        foreach (var type in Enum.GetValues<AgencyVerificationDocumentType>())
        {
            agency.VerificationDocuments.Add(Document(agency.Id, type));
        }
        var adminId = Guid.NewGuid();
        _agencies.Setup(repository => repository.GetByIdAsync(
                agency.Id,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(agency);
        var handler = new DecideAgencyVerificationCommandHandler(
            _agencies.Object,
            _auditLog.Object,
            _notifications.Object);

        var result = await handler.Handle(
            new DecideAgencyVerificationCommand(
                agency.Id,
                adminId,
                AgencyVerificationDecision.Approve,
                "Documents validated."),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        agency.VerificationStatus.Should().Be(AgencyVerificationStatus.Verified);
        agency.VerificationReviewedByUserId.Should().Be(adminId);
        agency.VerificationReviewedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(3));
        agency.VerificationDecisionReason.Should().BeNull(
            "approval must clear stale request-changes feedback");
        _agencies.Verify(repository => repository.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
        _auditLog.Verify(writer => writer.WriteAsync(
            adminId,
            "TravelAgency.Verification.Approve",
            "TravelAgency",
            agency.Id.ToString(),
            It.IsAny<object>(),
            AuditSeverity.Info,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Suspend_IsAllowedOnlyForVerifiedAgency()
    {
        var agency = Agency(Guid.NewGuid());
        _agencies.Setup(repository => repository.GetByIdAsync(
                agency.Id,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(agency);
        var handler = new DecideAgencyVerificationCommandHandler(
            _agencies.Object,
            _auditLog.Object,
            _notifications.Object);

        var result = await handler.Handle(
            new DecideAgencyVerificationCommand(
                agency.Id,
                Guid.NewGuid(),
                AgencyVerificationDecision.Suspend,
                "Compliance investigation."),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TravelAgencyErrors.InvalidVerificationTransition);
    }

    [Fact]
    public async Task DocumentDecline_StoresFeedbackReopensReviewAndNotifiesAgency()
    {
        var agency = Agency(Guid.NewGuid());
        agency.VerificationStatus = AgencyVerificationStatus.Verified;
        var document = Document(agency.Id, AgencyVerificationDocumentType.TaxCard);
        agency.VerificationDocuments.Add(document);
        _agencies.Setup(repository => repository.GetByIdAsync(
                agency.Id,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(agency);
        var handler = new DecideAgencyDocumentCommandHandler(
            _agencies.Object,
            _auditLog.Object,
            _notifications.Object);

        var result = await handler.Handle(
            new DecideAgencyDocumentCommand(
                agency.Id,
                Guid.NewGuid(),
                document.DocumentType,
                AgencyDocumentDecision.Decline,
                "The tax number is unreadable."),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        document.ReviewStatus.Should().Be(AgencyDocumentReviewStatus.Declined);
        document.ReviewReason.Should().Be("The tax number is unreadable.");
        agency.VerificationStatus.Should().Be(AgencyVerificationStatus.ChangesRequested);
        _notifications.Verify(service => service.NotifyUserAsync(
            agency.UserId,
            MarketplaceNotificationType.AgencyDocumentDeclined,
            It.IsAny<string>(),
            "The tax number is unreadable.",
            "/agency/verification",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteOnboarding_RequiresVerifiedEmail()
    {
        var users = new Mock<IUserRepository>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "agency@example.com",
            FullName = "Agency Owner",
            Role = UserRole.TravelAgency,
            IsEmailVerified = false
        };
        users.Setup(repository => repository.GetByIdAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var handler = new CompleteAgencyOnboardingCommandHandler(
            users.Object,
            _agencies.Object,
            _notifications.Object);

        var result = await handler.Handle(OnboardingCommand(user.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TravelAgencyErrors.EmailVerificationRequired);
        _agencies.Verify(repository => repository.TryAddAsync(
            It.IsAny<TravelAgency>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteOnboarding_CreatesPendingAgencyAndNotifiesReviewers()
    {
        var users = new Mock<IUserRepository>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "agency@example.com",
            FullName = "Agency Owner",
            Role = UserRole.TravelAgency,
            IsEmailVerified = true
        };
        users.Setup(repository => repository.GetByIdAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _agencies.Setup(repository => repository.TryAddAsync(
                It.IsAny<TravelAgency>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = new CompleteAgencyOnboardingCommandHandler(
            users.Object,
            _agencies.Object,
            _notifications.Object);

        var result = await handler.Handle(OnboardingCommand(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VerificationStatus.Should().Be(AgencyVerificationStatus.Pending);
        result.Value.CompanyName.Should().Be("Nile Horizon Travel");
        _notifications.Verify(service => service.NotifyAgencyReviewersAsync(
            MarketplaceNotificationType.AgencyProfileSubmitted,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<string>(url => url.StartsWith("/admin/agencies/")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static CompleteAgencyOnboardingCommand OnboardingCommand(Guid userId) =>
        new(
            userId,
            "  Nile   Horizon Travel  ",
            "Organized cultural and leisure tours.",
            "12 Nile Street",
            "Cairo",
            "Egypt",
            "+20212345678",
            "https://cdn.example.com/agency-logo.png",
            "https://agency.example.com");

    private static TravelAgency Agency(Guid userId)
    {
        var user = new User
        {
            Id = userId,
            FullName = "Agency Owner",
            Email = "owner@example.com",
            Role = UserRole.TravelAgency
        };
        var agency = new TravelAgency
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            User = user,
            CompanyName = "Example Travel",
            Description = "Example agency",
            Address = "1 Main Street",
            City = "Cairo",
            Country = "Egypt",
            CompanyPhone = "+20 100",
            CreatedAt = DateTime.UtcNow,
            VerificationStatus = AgencyVerificationStatus.Pending
        };
        user.TravelAgency = agency;
        return agency;
    }

    private static AgencyVerificationDocument Document(
        Guid agencyId,
        AgencyVerificationDocumentType type)
    {
        return new AgencyVerificationDocument
        {
            Id = Guid.NewGuid(),
            TravelAgencyId = agencyId,
            DocumentType = type,
            DocumentNumber = $"DOC-{type}",
            StorageReference = $"private/{Guid.NewGuid():N}.pdf",
            OriginalFileName = $"{type}.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 100,
            ExpiryDate = DateTime.UtcNow.Date.AddMonths(6),
            UploadedAt = DateTime.UtcNow,
            ReviewStatus = AgencyDocumentReviewStatus.Approved
        };
    }
}
