using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Repositories;

public sealed class TravelAgencyRepository : ITravelAgencyRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TravelAgencyRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TravelAgency?> GetByUserIdAsync(
        Guid userId,
        bool includeDocuments = false,
        CancellationToken cancellationToken = default)
    {
        var query = BaseQuery(includeDocuments);
        return query.FirstOrDefaultAsync(agency => agency.UserId == userId, cancellationToken);
    }

    public Task<TravelAgency?> GetByIdAsync(
        Guid agencyId,
        bool includeDocuments = false,
        CancellationToken cancellationToken = default)
    {
        var query = BaseQuery(includeDocuments);
        return query.FirstOrDefaultAsync(agency => agency.Id == agencyId, cancellationToken);
    }

    public Task<AgencyVerificationDocument?> GetDocumentAsync(
        Guid agencyId,
        AgencyVerificationDocumentType documentType,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.AgencyVerificationDocuments.FirstOrDefaultAsync(
            document => document.TravelAgencyId == agencyId &&
                document.DocumentType == documentType,
            cancellationToken);
    }

    public Task<bool> CompanyNameExistsAsync(
        string companyName,
        Guid excludingAgencyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TravelAgencies.AnyAsync(
            agency => agency.Id != excludingAgencyId &&
                agency.CompanyName == companyName,
            cancellationToken);
    }

    public Task<AgencyPayoutProfile?> GetPayoutProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _dbContext.AgencyPayoutProfiles
            .Include(profile => profile.TravelAgency)
            .FirstOrDefaultAsync(profile => profile.TravelAgency.UserId == userId, cancellationToken);

    public Task AddPayoutProfileAsync(AgencyPayoutProfile profile, CancellationToken cancellationToken = default) =>
        _dbContext.AgencyPayoutProfiles.AddAsync(profile, cancellationToken).AsTask();

    public async Task<IReadOnlyList<TravelAgency>> ListPayoutProfilesAsync(
        AgencyPayoutProfileStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TravelAgencies
            .Include(agency => agency.User)
            .Include(agency => agency.PayoutProfile)
            .Where(agency => agency.PayoutProfile != null);
        if (status is not null)
            query = query.Where(agency => agency.PayoutProfile!.Status == status);
        return await query.OrderBy(agency => agency.PayoutProfile!.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<TravelAgency>> ListAsync(
        AgencyVerificationStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TravelAgencies
            .AsNoTracking()
            .Include(agency => agency.User)
            .Include(agency => agency.VerificationDocuments)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(agency => agency.VerificationStatus == status);
        }

        var normalizedSearch = search?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(agency =>
                agency.CompanyName.Contains(normalizedSearch) ||
                agency.User.FullName.Contains(normalizedSearch) ||
                agency.User.Email.Contains(normalizedSearch) ||
                agency.City.Contains(normalizedSearch) ||
                agency.Country.Contains(normalizedSearch));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(agency => agency.UpdatedAt ?? agency.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TravelAgency>(items, page, pageSize, totalCount);
    }

    public async Task AddDocumentAsync(
        AgencyVerificationDocument document,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.AgencyVerificationDocuments.AddAsync(document, cancellationToken);
    }

    public async Task<bool> TryAddAsync(
        TravelAgency agency,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.TravelAgencies.AddAsync(agency, cancellationToken);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _dbContext.Entry(agency).State = EntityState.Detached;
            return false;
        }
    }

    public void RemoveDocument(AgencyVerificationDocument document)
    {
        _dbContext.AgencyVerificationDocuments.Remove(document);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<TravelAgency> BaseQuery(bool includeDocuments)
    {
        IQueryable<TravelAgency> query = _dbContext.TravelAgencies
            .Include(agency => agency.User)
            .Include(agency => agency.PayoutProfile);

        if (includeDocuments)
        {
            query = query.Include(agency => agency.VerificationDocuments);
        }

        return query;
    }
}
