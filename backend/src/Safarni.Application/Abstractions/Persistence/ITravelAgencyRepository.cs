using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Abstractions.Persistence;

public interface ITravelAgencyRepository
{
    Task<TravelAgency?> GetByUserIdAsync(
        Guid userId,
        bool includeDocuments = false,
        CancellationToken cancellationToken = default);

    Task<TravelAgency?> GetByIdAsync(
        Guid agencyId,
        bool includeDocuments = false,
        CancellationToken cancellationToken = default);

    Task<AgencyVerificationDocument?> GetDocumentAsync(
        Guid agencyId,
        AgencyVerificationDocumentType documentType,
        CancellationToken cancellationToken = default);

    Task<bool> CompanyNameExistsAsync(
        string companyName,
        Guid excludingAgencyId,
        CancellationToken cancellationToken = default);

    Task<AgencyPayoutProfile?> GetPayoutProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddPayoutProfileAsync(AgencyPayoutProfile profile, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TravelAgency>> ListPayoutProfilesAsync(AgencyPayoutProfileStatus? status, CancellationToken cancellationToken = default);

    Task<PagedResult<TravelAgency>> ListAsync(
        AgencyVerificationStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddDocumentAsync(
        AgencyVerificationDocument document,
        CancellationToken cancellationToken = default);

    Task<bool> TryAddAsync(
        TravelAgency agency,
        CancellationToken cancellationToken = default);

    void RemoveDocument(AgencyVerificationDocument document);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
