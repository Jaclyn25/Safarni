using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Agencies.Common;

namespace Safarni.Application.Features.Agencies.Dashboard;

public sealed record GetAgencyDashboardRecentActivityQuery(Guid UserId, int Count = 10)
    : IRequest<Result<IReadOnlyList<AgencyActivityItemResponse>>>;

public sealed class GetAgencyDashboardRecentActivityQueryHandler
    : IRequestHandler<GetAgencyDashboardRecentActivityQuery, Result<IReadOnlyList<AgencyActivityItemResponse>>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly IPackageBookingRequestRepository _bookingRequests;

    public GetAgencyDashboardRecentActivityQueryHandler(
        ITravelAgencyRepository agencies,
        IPackageBookingRequestRepository bookingRequests)
    {
        _agencies = agencies;
        _bookingRequests = bookingRequests;
    }

    public async Task<Result<IReadOnlyList<AgencyActivityItemResponse>>> Handle(
        GetAgencyDashboardRecentActivityQuery request,
        CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(request.UserId, true, cancellationToken);
        if (agency is null)
        {
            return Result<IReadOnlyList<AgencyActivityItemResponse>>.Failure(TravelAgencyErrors.NotFound);
        }

        var activities = new List<AgencyActivityItemResponse>();

        var recentBookings = await _bookingRequests.ListByAgencyAsync(agency.Id, cancellationToken);

        foreach (var b in recentBookings.OrderByDescending(b => b.CreatedAt).Take(request.Count))
        {
            var travelerName = b.TravelerUser?.FullName ?? "Traveler";
            var packageTitle = b.TravelPackage?.Title ?? "Travel Package";
            activities.Add(new AgencyActivityItemResponse(
                b.Id.ToString(),
                $"Booking request from {travelerName}",
                $"Requested {b.TravelerCount} seat(s) for package '{packageTitle}'",
                GetTimeAgo(b.CreatedAt),
                "booking",
                b.Status.ToString()
            ));
        }

        foreach (var doc in agency.VerificationDocuments)
        {
            activities.Add(new AgencyActivityItemResponse(
                doc.Id.ToString(),
                $"Document {AgencyResponseMapper.DisplayName(doc.DocumentType)}",
                $"Status: {doc.ReviewStatus}" + (string.IsNullOrWhiteSpace(doc.ReviewReason) ? "" : $" ({doc.ReviewReason})"),
                GetTimeAgo(doc.UploadedAt),
                "verification",
                doc.ReviewStatus.ToString()
            ));
        }

        var sorted = activities.Take(request.Count).ToList();
        return Result<IReadOnlyList<AgencyActivityItemResponse>>.Success(sorted);
    }

    private static string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime;
        if (span.TotalMinutes < 1) return "Just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 30) return $"{(int)span.TotalDays}d ago";
        return dateTime.ToString("MMM dd");
    }
}
