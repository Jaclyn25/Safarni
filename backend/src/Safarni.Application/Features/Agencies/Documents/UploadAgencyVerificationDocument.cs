using FluentValidation;
using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Storage;
using Safarni.Application.Abstractions.Notifications;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Agencies.Common;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Agencies.Documents;

public sealed record UploadAgencyVerificationDocumentCommand(
    Guid UserId,
    AgencyVerificationDocumentType DocumentType,
    string DocumentNumber,
    DateTime? ExpiryDate,
    string FileName,
    string ContentType,
    long FileSize,
    byte[] FileContent)
    : IRequest<Result<AgencyVerificationStatusResponse>>;

public sealed class UploadAgencyVerificationDocumentCommandValidator
    : AbstractValidator<UploadAgencyVerificationDocumentCommand>
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly string[] SupportedContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/png"
    ];

    public UploadAgencyVerificationDocumentCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.DocumentType).IsInEnum();
        RuleFor(command => command.DocumentNumber)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(command => command.ExpiryDate)
            .Must(date => date is null || date.Value.Date >= DateTime.UtcNow.Date)
            .WithMessage("Expiry date cannot be in the past.");
        RuleFor(command => command.FileName)
            .NotEmpty()
            .MaximumLength(255);
        RuleFor(command => command.ContentType)
            .NotEmpty()
            .Must(type => SupportedContentTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Only PDF, JPEG, and PNG verification documents are supported.");
        RuleFor(command => command.FileSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxFileSizeBytes);
        RuleFor(command => command.FileContent)
            .NotNull()
            .Must(content => content.Length > 0)
            .WithMessage("Verification document content is required.");
    }
}

public sealed class UploadAgencyVerificationDocumentCommandHandler
    : IRequestHandler<UploadAgencyVerificationDocumentCommand, Result<AgencyVerificationStatusResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly IAgencyVerificationDocumentStorage _storage;
    private readonly IMarketplaceNotificationService _notifications;

    public UploadAgencyVerificationDocumentCommandHandler(
        ITravelAgencyRepository agencies,
        IAgencyVerificationDocumentStorage storage,
        IMarketplaceNotificationService notifications)
    {
        _agencies = agencies;
        _storage = storage;
        _notifications = notifications;
    }

    public async Task<Result<AgencyVerificationStatusResponse>> Handle(
        UploadAgencyVerificationDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(
            request.UserId,
            includeDocuments: true,
            cancellationToken);

        if (agency is null)
        {
            return Result<AgencyVerificationStatusResponse>.Failure(TravelAgencyErrors.NotFound);
        }

        if (request.FileContent.LongLength != request.FileSize ||
            !TryValidateFile(request.FileName, request.ContentType, request.FileContent, out var extension))
        {
            return Result<AgencyVerificationStatusResponse>.Failure(TravelAgencyErrors.InvalidDocument);
        }

        var existing = agency.VerificationDocuments
            .SingleOrDefault(document => document.DocumentType == request.DocumentType);
        var oldStorageReference = existing?.StorageReference;
        var isReplacement = existing is not null;

        await using var contentStream = new MemoryStream(request.FileContent, writable: false);
        var newStorageReference = await _storage.SaveAsync(
            agency.Id,
            contentStream,
            extension,
            cancellationToken);

        var now = DateTime.UtcNow;
        try
        {
            if (existing is null)
            {
                existing = new AgencyVerificationDocument
                {
                    Id = Guid.NewGuid(),
                    TravelAgencyId = agency.Id,
                    DocumentType = request.DocumentType,
                    UploadedAt = now
                };
                agency.VerificationDocuments.Add(existing);
                await _agencies.AddDocumentAsync(existing, cancellationToken);
            }
            else
            {
                existing.UpdatedAt = now;
            }

            existing.DocumentNumber = request.DocumentNumber.Trim();
            existing.ExpiryDate = request.ExpiryDate?.Date;
            existing.OriginalFileName = Path.GetFileName(request.FileName.Trim());
            existing.ContentType = NormalizeContentType(request.ContentType);
            existing.FileSizeBytes = request.FileSize;
            existing.StorageReference = newStorageReference;
            existing.ReviewStatus = AgencyDocumentReviewStatus.Pending;
            existing.ReviewReason = null;
            existing.ReviewedAt = null;
            existing.ReviewedByUserId = null;

            ReopenReviewWhenVerified(agency);
            agency.UpdatedAt = now;
            await _agencies.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _storage.DeleteAsync(newStorageReference, CancellationToken.None);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(oldStorageReference))
        {
            await _storage.DeleteAsync(oldStorageReference, cancellationToken);
        }

        var documentName = AgencyResponseMapper.DisplayName(request.DocumentType);
        await _notifications.NotifyAgencyReviewersAsync(
            isReplacement
                ? MarketplaceNotificationType.AgencyDocumentReplaced
                : MarketplaceNotificationType.AgencyDocumentSubmitted,
            isReplacement ? "Agency document replaced" : "New agency document submitted",
            $"{agency.CompanyName} {(isReplacement ? "replaced" : "uploaded")} its {documentName}.",
            $"/admin/agencies/{agency.Id}",
            cancellationToken);

        return Result<AgencyVerificationStatusResponse>.Success(
            AgencyResponseMapper.ToVerificationStatus(agency));
    }

    private static void ReopenReviewWhenVerified(TravelAgency agency)
    {
        if (agency.VerificationStatus != AgencyVerificationStatus.Verified)
        {
            return;
        }

        agency.VerificationStatus = AgencyVerificationStatus.ChangesRequested;
        agency.VerificationDecisionReason =
            "A verification document was replaced and requires administrator review.";
        agency.VerificationReviewedAt = null;
        agency.VerificationReviewedByUserId = null;
    }

    private static string NormalizeContentType(string contentType)
    {
        return contentType.Equals("image/jpg", StringComparison.OrdinalIgnoreCase)
            ? "image/jpeg"
            : contentType.ToLowerInvariant();
    }

    private static bool TryValidateFile(
        string fileName,
        string contentType,
        byte[] content,
        out string extension)
    {
        extension = Path.GetExtension(fileName).ToLowerInvariant();
        var normalizedType = NormalizeContentType(contentType);

        return extension switch
        {
            ".pdf" => normalizedType == "application/pdf" &&
                content.Length >= 5 &&
                content.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            ".jpg" or ".jpeg" => normalizedType == "image/jpeg" &&
                content.Length >= 3 &&
                content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF,
            ".png" => normalizedType == "image/png" &&
                content.Length >= 8 &&
                content.AsSpan(0, 8).SequenceEqual(
                    new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            _ => false
        };
    }
}
