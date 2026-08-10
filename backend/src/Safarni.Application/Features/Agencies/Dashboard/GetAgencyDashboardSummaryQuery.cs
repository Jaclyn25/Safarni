using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Billing;
using Safarni.Application.Features.Agencies.Common;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Agencies.Dashboard;

public sealed record GetAgencyDashboardSummaryQuery(Guid UserId)
    : IRequest<Result<AgencyDashboardSummaryResponse>>;

public sealed class GetAgencyDashboardSummaryQueryHandler
    : IRequestHandler<GetAgencyDashboardSummaryQuery, Result<AgencyDashboardSummaryResponse>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly ITravelPackageRepository _packages;
    private readonly IPackageBookingRequestRepository _bookingRequests;
    private readonly IBillingRepository _billing;

    public GetAgencyDashboardSummaryQueryHandler(
        ITravelAgencyRepository agencies,
        ITravelPackageRepository packages,
        IPackageBookingRequestRepository bookingRequests,
        IBillingRepository billing)
    {
        _agencies = agencies;
        _packages = packages;
        _bookingRequests = bookingRequests;
        _billing = billing;
    }

    public async Task<Result<AgencyDashboardSummaryResponse>> Handle(
        GetAgencyDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(
            request.UserId,
            includeDocuments: true,
            cancellationToken);

        if (agency is null)
        {
            return Result<AgencyDashboardSummaryResponse>.Failure(TravelAgencyErrors.NotFound);
        }

        var packagesList = await _packages.ListByAgencyAsync(agency.Id, cancellationToken);
        var requestsList = await _bookingRequests.ListByAgencyAsync(agency.Id, cancellationToken);
        var subscription = await _billing.GetAgencySubscriptionAsync(
            agency.Id,
            includePayments: true,
            cancellationToken);
        if (subscription is not null)
        {
            BillingMapping.RefreshLifecycle(subscription, DateTime.UtcNow);
            if (subscription.UpdatedAt is not null)
            {
                await _billing.SaveChangesAsync(cancellationToken);
            }
        }
        var subscriptionPayments = await _billing.ListPaymentsAsync(agency.Id, cancellationToken);

        var totalPackages = packagesList.Count;
        var publishedPackages = packagesList.Count(p => p.IsPublished && p.IsActive);
        var draftPackages = packagesList.Count(p => !p.IsPublished && p.IsActive);

        var totalRequests = requestsList.Count;
        var pendingRequests = requestsList.Count(b => b.Status == PackageBookingRequestStatus.Pending);
        var acceptedRequests = requestsList.Count(b => b.Status == PackageBookingRequestStatus.Accepted);
        var rejectedRequests = requestsList.Count(b => b.Status == PackageBookingRequestStatus.Rejected);

        var seatsSold = requestsList
            .Where(b => b.Status == PackageBookingRequestStatus.Accepted)
            .Sum(b => b.TravelerCount);

        var seatsAvailable = packagesList
            .Where(p => p.IsPublished && p.IsActive)
            .Sum(p => p.AvailableSeats);

        var acceptedBookingValue = requestsList
            .Where(b => b.Status == PackageBookingRequestStatus.Accepted)
            .Sum(b => b.TravelerCount * b.TravelPackage.Price);
        var bookingCurrencyCode = packagesList
            .Select(package => package.CurrencyCode)
            .FirstOrDefault(currency => !string.IsNullOrWhiteSpace(currency))
            ?? "USD";
        var successfulSubscriptionPayments = subscriptionPayments
            .Where(payment => payment.Status == PaymentStatus.Succeeded)
            .ToArray();

        var uploadedDocs = agency.VerificationDocuments.Count(d => d.ReviewStatus == AgencyDocumentReviewStatus.Approved || d.ReviewStatus == AgencyDocumentReviewStatus.Pending);
        var verificationProgress = $"{uploadedDocs}/3";

        var response = new AgencyDashboardSummaryResponse(
            agency.Id,
            agency.CompanyName,
            totalPackages,
            publishedPackages,
            draftPackages,
            totalRequests,
            pendingRequests,
            acceptedRequests,
            rejectedRequests,
            seatsSold,
            seatsAvailable,
            agency.VerificationStatus.ToString(),
            verificationProgress,
            agency.VerificationStatus == AgencyVerificationStatus.Verified,
            DateTime.UtcNow,
            acceptedBookingValue,
            bookingCurrencyCode,
            subscription?.Plan.DisplayName,
            subscription?.Status,
            subscription?.Plan.Price,
            subscription?.Plan.CurrencyCode,
            successfulSubscriptionPayments.Sum(payment => payment.Amount),
            successfulSubscriptionPayments.Length,
            subscriptionPayments.Count(payment => payment.Status == PaymentStatus.Failed),
            subscription?.CurrentPeriodEnd);

        return Result<AgencyDashboardSummaryResponse>.Success(response);
    }
}
