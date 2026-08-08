using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Storage;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Agencies.Common;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Agencies.Documents;

public sealed record GetAgencyVerificationDocumentContentQuery(
    Guid RequestingUserId,
    Guid AgencyId,
    AgencyVerificationDocumentType DocumentType,
    bool IsAuthorizedAdmin)
    : IRequest<Result<AgencyVerificationDocumentContentResponse>>;

public sealed class GetAgencyVerificationDocumentContentQueryHandler
    : IRequestHandler<GetAgencyVerificationDocumentContentQuery, Result<AgencyVerificationDocumentContentResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly IAgencyVerificationDocumentStorage _storage;

    public GetAgencyVerificationDocumentContentQueryHandler(
        ITravelAgencyRepository agencies,
        IAgencyVerificationDocumentStorage storage)
    {
        _agencies = agencies;
        _storage = storage;
    }

    public async Task<Result<AgencyVerificationDocumentContentResponse>> Handle(
        GetAgencyVerificationDocumentContentQuery request,
        CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByIdAsync(request.AgencyId, cancellationToken: cancellationToken);
        if (agency is null)
        {
            return Result<AgencyVerificationDocumentContentResponse>.Failure(TravelAgencyErrors.NotFound);
        }

        if (!request.IsAuthorizedAdmin && agency.UserId != request.RequestingUserId)
        {
            return Result<AgencyVerificationDocumentContentResponse>.Failure(
                TravelAgencyErrors.DocumentAccessDenied);
        }

        var document = await _agencies.GetDocumentAsync(
            agency.Id,
            request.DocumentType,
            cancellationToken);
        if (document is null)
        {
            return Result<AgencyVerificationDocumentContentResponse>.Failure(
                TravelAgencyErrors.DocumentNotFound);
        }

        await using var stream = await _storage.OpenReadAsync(
            document.StorageReference,
            cancellationToken);
        if (stream is null)
        {
            return Result<AgencyVerificationDocumentContentResponse>.Failure(
                TravelAgencyErrors.DocumentNotFound);
        }

        await using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);

        return Result<AgencyVerificationDocumentContentResponse>.Success(
            new AgencyVerificationDocumentContentResponse(
                memory.ToArray(),
                document.ContentType,
                document.OriginalFileName));
    }
}
