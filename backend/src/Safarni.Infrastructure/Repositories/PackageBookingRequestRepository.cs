using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Repositories;

public sealed class PackageBookingRequestRepository : IPackageBookingRequestRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PackageBookingRequestRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PackageBookingRequest?> GetByIdForTravelerAsync(Guid requestId, Guid travelerUserId, CancellationToken cancellationToken = default)
    {
        return BaseQuery().FirstOrDefaultAsync(request =>
            request.Id == requestId && request.TravelerUserId == travelerUserId, cancellationToken);
    }

    public Task<PackageBookingRequest?> GetByIdForAgencyAsync(Guid requestId, Guid agencyId, CancellationToken cancellationToken = default)
    {
        return BaseQuery().FirstOrDefaultAsync(request =>
            request.Id == requestId && request.TravelPackage.TravelAgencyId == agencyId, cancellationToken);
    }

    public async Task<IReadOnlyList<PackageBookingRequest>> ListByTravelerAsync(Guid travelerUserId, CancellationToken cancellationToken = default)
    {
        return await BaseQuery()
            .Where(request => request.TravelerUserId == travelerUserId)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PackageBookingRequest>> ListByAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default)
    {
        return await BaseQuery()
            .Where(request => request.TravelPackage.TravelAgencyId == agencyId)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetAcceptedTravelerCountAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        var count = await _dbContext.PackageBookingRequests
            .Where(request => request.TravelPackageId == packageId &&
                (request.Status == PackageBookingRequestStatus.Accepted ||
                 request.Status == PackageBookingRequestStatus.AwaitingPayment ||
                 request.Status == PackageBookingRequestStatus.Confirmed ||
                 request.Status == PackageBookingRequestStatus.InProgress ||
                 request.Status == PackageBookingRequestStatus.Completed))
            .SumAsync(request => (int?)request.TravelerCount, cancellationToken);
        return count ?? 0;
    }

    public Task<bool> HasOpenRequestAsync(Guid packageId, Guid travelerUserId, CancellationToken cancellationToken = default)
    {
        return _dbContext.PackageBookingRequests.AnyAsync(request =>
            request.TravelPackageId == packageId &&
            request.TravelerUserId == travelerUserId &&
            (request.Status == PackageBookingRequestStatus.Pending ||
             request.Status == PackageBookingRequestStatus.Accepted ||
             request.Status == PackageBookingRequestStatus.OfferSent ||
             request.Status == PackageBookingRequestStatus.AwaitingPayment ||
             request.Status == PackageBookingRequestStatus.Confirmed ||
             request.Status == PackageBookingRequestStatus.InProgress),
            cancellationToken);
    }

    public Task<int> CountAcceptedByAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default) =>
        _dbContext.PackageBookingRequests.CountAsync(request =>
            request.TravelPackage.TravelAgencyId == agencyId &&
            (request.Status == PackageBookingRequestStatus.Accepted ||
             request.Status == PackageBookingRequestStatus.Confirmed ||
             request.Status == PackageBookingRequestStatus.InProgress ||
             request.Status == PackageBookingRequestStatus.Completed),
            cancellationToken);

    public async Task<int> SumAcceptedTravelersByAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default) =>
        await _dbContext.PackageBookingRequests
            .Where(request => request.TravelPackage.TravelAgencyId == agencyId &&
                (request.Status == PackageBookingRequestStatus.Accepted ||
                 request.Status == PackageBookingRequestStatus.Confirmed ||
                 request.Status == PackageBookingRequestStatus.InProgress ||
                 request.Status == PackageBookingRequestStatus.Completed))
            .SumAsync(request => (int?)request.TravelerCount, cancellationToken) ?? 0;

    public Task AddAsync(PackageBookingRequest request, CancellationToken cancellationToken = default)
    {
        return _dbContext.PackageBookingRequests.AddAsync(request, cancellationToken).AsTask();
    }

    public Task AddOfferAsync(PackageBookingOffer offer, CancellationToken cancellationToken = default)
    {
        return _dbContext.PackageBookingOffers.AddAsync(offer, cancellationToken).AsTask();
    }

    public Task<BookingPayment?> GetPaymentBySessionAsync(string provider, string sessionId, CancellationToken cancellationToken = default)
    {
        return _dbContext.BookingPayments
            .Include(payment => payment.PackageBookingRequest)
                .ThenInclude(request => request.TravelPackage)
                    .ThenInclude(package => package.TravelAgency)
            .Include(payment => payment.PackageBookingRequest)
                .ThenInclude(request => request.Payouts)
            .Include(payment => payment.PackageBookingRequest)
                .ThenInclude(request => request.Payments)
            .Include(payment => payment.PackageBookingOffer)
            .FirstOrDefaultAsync(payment => payment.Provider == provider && payment.PaymentSessionId == sessionId, cancellationToken);
    }

    public Task AddPaymentAsync(BookingPayment payment, CancellationToken cancellationToken = default)
    {
        return _dbContext.BookingPayments.AddAsync(payment, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<PackageBookingMessage>> ListMessagesAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PackageBookingMessages
            .Include(message => message.SenderUser)
            .Where(message => message.PackageBookingRequestId == requestId)
            .OrderBy(message => message.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task AddMessageAsync(PackageBookingMessage message, CancellationToken cancellationToken = default)
    {
        return _dbContext.PackageBookingMessages.AddAsync(message, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<PackageBookingTraveler>> ListTravelersAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        await _dbContext.PackageBookingTravelers
            .Include(traveler => traveler.Documents)
            .Where(traveler => traveler.PackageBookingRequestId == requestId)
            .OrderBy(traveler => traveler.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<PackageBookingTraveler?> GetTravelerAsync(Guid requestId, Guid travelerId, CancellationToken cancellationToken = default) =>
        _dbContext.PackageBookingTravelers
            .Include(traveler => traveler.Documents)
            .FirstOrDefaultAsync(traveler => traveler.PackageBookingRequestId == requestId && traveler.Id == travelerId, cancellationToken);

    public Task AddTravelerAsync(PackageBookingTraveler traveler, CancellationToken cancellationToken = default) =>
        _dbContext.PackageBookingTravelers.AddAsync(traveler, cancellationToken).AsTask();

    public async Task<IReadOnlyList<PackageBookingDocument>> ListDocumentsAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        await _dbContext.PackageBookingDocuments
            .Where(document => document.PackageBookingRequestId == requestId)
            .OrderByDescending(document => document.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<PackageBookingDocument?> GetDocumentAsync(Guid requestId, Guid documentId, CancellationToken cancellationToken = default) =>
        _dbContext.PackageBookingDocuments.FirstOrDefaultAsync(
            document => document.PackageBookingRequestId == requestId && document.Id == documentId, cancellationToken);

    public Task AddDocumentAsync(PackageBookingDocument document, CancellationToken cancellationToken = default) =>
        _dbContext.PackageBookingDocuments.AddAsync(document, cancellationToken).AsTask();

    public Task<BookingSupplierEvidence?> GetEvidenceAsync(Guid evidenceId, CancellationToken cancellationToken = default) =>
        _dbContext.BookingSupplierEvidence
            .Include(evidence => evidence.PackageBookingRequest)
                .ThenInclude(request => request.TravelPackage)
                    .ThenInclude(package => package.TravelAgency)
            .Include(evidence => evidence.PackageBookingOffer)
            .Include(evidence => evidence.TravelAgency)
            .FirstOrDefaultAsync(evidence => evidence.Id == evidenceId, cancellationToken);

    public async Task<IReadOnlyList<BookingSupplierEvidence>> ListPendingEvidenceAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.BookingSupplierEvidence
            .Include(evidence => evidence.PackageBookingRequest)
                .ThenInclude(request => request.TravelPackage)
            .Include(evidence => evidence.TravelAgency)
            .Where(evidence => evidence.Status == BookingSupplierEvidenceStatus.PendingReview)
            .OrderBy(evidence => evidence.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task AddEvidenceAsync(BookingSupplierEvidence evidence, CancellationToken cancellationToken = default) =>
        _dbContext.BookingSupplierEvidence.AddAsync(evidence, cancellationToken).AsTask();

    public Task<BookingPayout?> GetPayoutByEvidenceAsync(Guid evidenceId, CancellationToken cancellationToken = default) =>
        _dbContext.BookingPayouts.FirstOrDefaultAsync(payout => payout.SupplierEvidenceId == evidenceId, cancellationToken);

    public Task<BookingPayout?> GetPayoutAsync(Guid payoutId, CancellationToken cancellationToken = default) =>
        _dbContext.BookingPayouts
            .Include(payout => payout.TravelAgency)
                .ThenInclude(agency => agency.PayoutProfile)
            .Include(payout => payout.PackageBookingRequest)
                .ThenInclude(request => request.TravelPackage)
            .Include(payout => payout.SupplierEvidence)
            .FirstOrDefaultAsync(payout => payout.Id == payoutId, cancellationToken);

    public async Task<IReadOnlyList<BookingPayout>> ListHeldPayoutsReadyForReleaseAsync(
        DateTime asOfUtc,
        CancellationToken cancellationToken = default) =>
        await _dbContext.BookingPayouts
            .Include(payout => payout.TravelAgency)
                .ThenInclude(agency => agency.PayoutProfile)
            .Include(payout => payout.PackageBookingRequest)
                .ThenInclude(request => request.TravelPackage)
            .Where(payout => payout.Status == BookingPayoutStatus.Held &&
                payout.ReleaseAt != null && payout.ReleaseAt <= asOfUtc)
            .OrderBy(payout => payout.ReleaseAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BookingPayout>> ListActionablePayoutsAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.BookingPayouts
            .Include(payout => payout.TravelAgency)
            .Include(payout => payout.PackageBookingRequest)
                .ThenInclude(request => request.TravelPackage)
            .Where(payout => payout.Status == BookingPayoutStatus.Approved || payout.Status == BookingPayoutStatus.Held)
            .OrderBy(payout => payout.ReleaseAt)
            .ThenBy(payout => payout.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task AddPayoutAsync(BookingPayout payout, CancellationToken cancellationToken = default) =>
        _dbContext.BookingPayouts.AddAsync(payout, cancellationToken).AsTask();

    public Task<BookingDispute?> GetDisputeAsync(Guid disputeId, CancellationToken cancellationToken = default) =>
        _dbContext.BookingDisputes
            .Include(dispute => dispute.PackageBookingRequest)
                .ThenInclude(request => request.TravelPackage)
                    .ThenInclude(package => package.TravelAgency)
            .Include(dispute => dispute.PackageBookingRequest)
                .ThenInclude(request => request.Payments)
            .Include(dispute => dispute.PackageBookingRequest)
                .ThenInclude(request => request.Payouts)
            .FirstOrDefaultAsync(dispute => dispute.Id == disputeId, cancellationToken);

    public async Task<IReadOnlyList<BookingDispute>> ListDisputesAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.BookingDisputes
            .Include(dispute => dispute.PackageBookingRequest)
                .ThenInclude(request => request.TravelPackage)
            .OrderByDescending(dispute => dispute.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task AddDisputeAsync(BookingDispute dispute, CancellationToken cancellationToken = default) =>
        _dbContext.BookingDisputes.AddAsync(dispute, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<PackageBookingRequest> BaseQuery()
    {
        return _dbContext.PackageBookingRequests
            .Include(request => request.TravelPackage)
                .ThenInclude(package => package.TravelAgency)
                    .ThenInclude(agency => agency.User)
            .Include(request => request.TravelerUser)
            .Include(request => request.AgencyReview)
            .Include(request => request.Offers)
            .Include(request => request.Payments)
            .Include(request => request.Travelers)
                .ThenInclude(traveler => traveler.Documents)
            .Include(request => request.Documents)
            .Include(request => request.Disputes);
}
}
