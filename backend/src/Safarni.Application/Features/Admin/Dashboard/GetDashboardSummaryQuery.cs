using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Admin.Dashboard;

public sealed record GetDashboardSummaryQuery()
    : IRequest<Result<DashboardSummaryResponse>>;

public sealed record DashboardSummaryResponse(
    int TotalUsers,
    int ActiveUsers,
    int VerifiedUsers,
    int TotalTrips,
    int TotalVisaApplications,
    int TotalConversations,
    int TotalAiMessages,
    int ItineraryJobsTotal,
    int ItineraryJobsPending,
    int ItineraryJobsFailed,
    int RecentAiFailuresLast24Hours,
    bool QdrantHealthy,
    int DegradedServiceCount);

public sealed class GetDashboardSummaryQueryHandler
    : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryResponse>>
{
    private readonly IAdminRepository _adminRepository;

    public GetDashboardSummaryQueryHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task<Result<DashboardSummaryResponse>> Handle(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var summary = await _adminRepository.GetDashboardSummaryAsync(cancellationToken);

        return Result<DashboardSummaryResponse>.Success(new DashboardSummaryResponse(
            summary.TotalUsers,
            summary.ActiveUsers,
            summary.VerifiedUsers,
            summary.TotalTrips,
            summary.TotalVisaApplications,
            summary.TotalConversations,
            summary.TotalAiMessages,
            summary.ItineraryJobsTotal,
            summary.ItineraryJobsPending,
            summary.ItineraryJobsFailed,
            summary.RecentAiFailuresLast24Hours,
            summary.QdrantHealthy,
            summary.DegradedServiceCount));
    }
}
