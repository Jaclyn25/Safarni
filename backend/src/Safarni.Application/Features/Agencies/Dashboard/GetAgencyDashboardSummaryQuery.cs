using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
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

    public GetAgencyDashboardSummaryQueryHandler(
        ITravelAgencyRepository agencies,
        ITravelPackageRepository packages,
        IPackageBookingRequestRepository bookingRequests)
    {
        _agencies = agencies;
        _packages = packages;
        _bookingRequests = bookingRequests;
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
            DateTime.UtcNow);

        return Result<AgencyDashboardSummaryResponse>.Success(response);
    }
}
