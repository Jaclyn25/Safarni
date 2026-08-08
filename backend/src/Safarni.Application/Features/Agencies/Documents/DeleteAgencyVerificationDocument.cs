using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Storage;
using Safarni.Application.Abstractions.Notifications;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Agencies.Common;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Agencies.Documents;

public sealed record DeleteAgencyVerificationDocumentCommand(
    Guid UserId,
    AgencyVerificationDocumentType DocumentType)
    : IRequest<Result<AgencyVerificationStatusResponse>>;

public sealed class DeleteAgencyVerificationDocumentCommandHandler
    : IRequestHandler<DeleteAgencyVerificationDocumentCommand, Result<AgencyVerificationStatusResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly IAgencyVerificationDocumentStorage _storage;
    private readonly IMarketplaceNotificationService _notifications;

    public DeleteAgencyVerificationDocumentCommandHandler(
        ITravelAgencyRepository agencies,
        IAgencyVerificationDocumentStorage storage,
        IMarketplaceNotificationService notifications)
    {
        _agencies = agencies;
        _storage = storage;
        _notifications = notifications;
    }

    public async Task<Result<AgencyVerificationStatusResponse>> Handle(
        DeleteAgencyVerificationDocumentCommand request,
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

        var document = agency.VerificationDocuments
            .SingleOrDefault(item => item.DocumentType == request.DocumentType);
        if (document is null)
        {
            return Result<AgencyVerificationStatusResponse>.Failure(TravelAgencyErrors.DocumentNotFound);
        }

        var storageReference = document.StorageReference;
        agency.VerificationDocuments.Remove(document);
        _agencies.RemoveDocument(document);

        if (agency.VerificationStatus == AgencyVerificationStatus.Verified)
        {
            agency.VerificationStatus = AgencyVerificationStatus.ChangesRequested;
            agency.VerificationDecisionReason =
                "A required verification document was removed and must be uploaded again.";
            agency.VerificationReviewedAt = null;
            agency.VerificationReviewedByUserId = null;
        }

        agency.UpdatedAt = DateTime.UtcNow;
        await _agencies.SaveChangesAsync(cancellationToken);
        await _storage.DeleteAsync(storageReference, cancellationToken);

        await _notifications.NotifyAgencyReviewersAsync(
            MarketplaceNotificationType.AgencyDocumentRemoved,
            "Agency document removed",
            $"{agency.CompanyName} removed its {AgencyResponseMapper.DisplayName(request.DocumentType)}.",
            $"/admin/agencies/{agency.Id}",
            cancellationToken);

        return Result<AgencyVerificationStatusResponse>.Success(
            AgencyResponseMapper.ToVerificationStatus(agency));
    }
}
