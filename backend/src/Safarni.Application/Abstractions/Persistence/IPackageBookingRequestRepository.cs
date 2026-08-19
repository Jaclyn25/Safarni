using Safarni.Domain.Entities;

namespace Safarni.Application.Abstractions.Persistence;

public interface IPackageBookingRequestRepository
{
    Task<PackageBookingRequest?> GetByIdForTravelerAsync(Guid requestId, Guid travelerUserId, CancellationToken cancellationToken = default);
    Task<PackageBookingRequest?> GetByIdForAgencyAsync(Guid requestId, Guid agencyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PackageBookingRequest>> ListByTravelerAsync(Guid travelerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PackageBookingRequest>> ListByAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default);
    Task<int> GetAcceptedTravelerCountAsync(Guid packageId, CancellationToken cancellationToken = default);
    Task<bool> HasOpenRequestAsync(Guid packageId, Guid travelerUserId, CancellationToken cancellationToken = default);
    Task<int> CountAcceptedByAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default);
    Task<int> SumAcceptedTravelersByAgencyAsync(Guid agencyId, CancellationToken cancellationToken = default);
    Task AddAsync(PackageBookingRequest request, CancellationToken cancellationToken = default);
    Task AddOfferAsync(PackageBookingOffer offer, CancellationToken cancellationToken = default);
    Task<BookingPayment?> GetPaymentBySessionAsync(string provider, string sessionId, CancellationToken cancellationToken = default);
    Task AddPaymentAsync(BookingPayment payment, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PackageBookingMessage>> ListMessagesAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task AddMessageAsync(PackageBookingMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PackageBookingTraveler>> ListTravelersAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<PackageBookingTraveler?> GetTravelerAsync(Guid requestId, Guid travelerId, CancellationToken cancellationToken = default);
    Task AddTravelerAsync(PackageBookingTraveler traveler, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PackageBookingDocument>> ListDocumentsAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<PackageBookingDocument?> GetDocumentAsync(Guid requestId, Guid documentId, CancellationToken cancellationToken = default);
    Task AddDocumentAsync(PackageBookingDocument document, CancellationToken cancellationToken = default);
    Task<BookingSupplierEvidence?> GetEvidenceAsync(Guid evidenceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookingSupplierEvidence>> ListPendingEvidenceAsync(CancellationToken cancellationToken = default);
    Task AddEvidenceAsync(BookingSupplierEvidence evidence, CancellationToken cancellationToken = default);
    Task<BookingPayout?> GetPayoutByEvidenceAsync(Guid evidenceId, CancellationToken cancellationToken = default);
    Task<BookingPayout?> GetPayoutAsync(Guid payoutId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookingPayout>> ListHeldPayoutsReadyForReleaseAsync(DateTime asOfUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookingPayout>> ListActionablePayoutsAsync(CancellationToken cancellationToken = default);
    Task AddPayoutAsync(BookingPayout payout, CancellationToken cancellationToken = default);
    Task<BookingDispute?> GetDisputeAsync(Guid disputeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookingDispute>> ListDisputesAsync(CancellationToken cancellationToken = default);
    Task AddDisputeAsync(BookingDispute dispute, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
