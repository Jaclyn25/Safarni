using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Agencies.Common;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Agencies.Dashboard;

public sealed record GetAgencyDashboardTrendsQuery(Guid UserId, int Days = 30)
    : IRequest<Result<IReadOnlyList<AgencyDashboardTrendPointResponse>>>;

public sealed class GetAgencyDashboardTrendsQueryHandler
    : IRequestHandler<GetAgencyDashboardTrendsQuery, Result<IReadOnlyList<AgencyDashboardTrendPointResponse>>>
{
    private readonly ITravelAgencyRepository _agencies;
    private readonly IPackageBookingRequestRepository _bookingRequests;

    public GetAgencyDashboardTrendsQueryHandler(
        ITravelAgencyRepository agencies,
        IPackageBookingRequestRepository bookingRequests)
    {
        _agencies = agencies;
        _bookingRequests = bookingRequests;
    }

    public async Task<Result<IReadOnlyList<AgencyDashboardTrendPointResponse>>> Handle(
        GetAgencyDashboardTrendsQuery request,
        CancellationToken cancellationToken)
    {
        var agency = await _agencies.GetByUserIdAsync(request.UserId, false, cancellationToken);
        if (agency is null)
        {
            return Result<IReadOnlyList<AgencyDashboardTrendPointResponse>>.Failure(TravelAgencyErrors.NotFound);
        }

        var startDate = DateTime.UtcNow.Date.AddDays(-Math.Abs(request.Days));
        var allRequests = await _bookingRequests.ListByAgencyAsync(agency.Id, cancellationToken);
        var filteredRequests = allRequests.Where(r => r.CreatedAt >= startDate).ToList();

        var trendPoints = new List<AgencyDashboardTrendPointResponse>();
        for (int i = request.Days - 1; i >= 0; i--)
        {
            var date = DateTime.UtcNow.Date.AddDays(-i);
            var dateStr = date.ToString("MMM dd");
            var dayRequests = filteredRequests.Where(r => r.CreatedAt.Date == date).ToList();

            trendPoints.Add(new AgencyDashboardTrendPointResponse(
                dateStr,
                dayRequests.Count,
                dayRequests.Count(r => r.Status == PackageBookingRequestStatus.Accepted),
                dayRequests.Count(r => r.Status == PackageBookingRequestStatus.Rejected)
            ));
        }

        return Result<IReadOnlyList<AgencyDashboardTrendPointResponse>>.Success(trendPoints);
    }
}
