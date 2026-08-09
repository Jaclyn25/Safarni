using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Common;

namespace Safarni.Application.Features.Admin.Analytics;

public sealed record GetAnalyticsOverviewQuery(DateTime From, DateTime To)
    : IRequest<Result<AnalyticsOverviewResponse>>;

public sealed record AnalyticsDailyPointResponse(
    string Date,
    int Users,
    int Trips,
    int Messages,
    int ItineraryJobs);

public sealed record AnalyticsOverviewResponse(
    IReadOnlyList<AnalyticsDailyPointResponse> Points);

public sealed class GetAnalyticsOverviewQueryHandler
    : IRequestHandler<GetAnalyticsOverviewQuery, Result<AnalyticsOverviewResponse>>
{
    private readonly IAdminRepository _adminRepository;

    public GetAnalyticsOverviewQueryHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task<Result<AnalyticsOverviewResponse>> Handle(
        GetAnalyticsOverviewQuery request,
        CancellationToken cancellationToken)
    {
        if (request.From > request.To)
        {
            return Result<AnalyticsOverviewResponse>.Failure(
                AdminErrors.InvalidDateRange);
        }

        var from = request.From.Date;
        var to = request.To.Date;

        if (to - from > TimeSpan.FromDays(366))
        {
            to = from.AddDays(366);
        }

        var data = await _adminRepository.GetAnalyticsOverviewAsync(
            from,
            to,
            cancellationToken);

        return Result<AnalyticsOverviewResponse>.Success(
            new AnalyticsOverviewResponse(
                data.Points
                    .Select(point => new AnalyticsDailyPointResponse(
                        point.Date.ToString("yyyy-MM-dd"),
                        point.Users,
                        point.Trips,
                        point.Messages,
                        point.ItineraryJobs))
                    .ToArray()));
    }
}

public sealed record GetAnalyticsFunnelsQuery()
    : IRequest<Result<AnalyticsFunnelResponse>>;

public sealed record AnalyticsFunnelResponse(
    int RegisteredUsers,
    int VerifiedUsers,
    int UsersWithTrips,
    int UsersWithGeneratedItineraries);

public sealed class GetAnalyticsFunnelsQueryHandler
    : IRequestHandler<GetAnalyticsFunnelsQuery, Result<AnalyticsFunnelResponse>>
{
    private readonly IAdminRepository _adminRepository;

    public GetAnalyticsFunnelsQueryHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task<Result<AnalyticsFunnelResponse>> Handle(
        GetAnalyticsFunnelsQuery request,
        CancellationToken cancellationToken)
    {
        var funnel = await _adminRepository.GetAnalyticsFunnelAsync(cancellationToken);

        return Result<AnalyticsFunnelResponse>.Success(new AnalyticsFunnelResponse(
            funnel.RegisteredUsers,
            funnel.VerifiedUsers,
            funnel.UsersWithTrips,
            funnel.UsersWithGeneratedItineraries));
    }
}

public sealed record GetTopDestinationsQuery(int Limit = 10)
    : IRequest<Result<IReadOnlyList<TopDestinationResponse>>>;

public sealed record TopDestinationResponse(string Name, int TripCount);

public sealed class GetTopDestinationsQueryHandler
    : IRequestHandler<GetTopDestinationsQuery, Result<IReadOnlyList<TopDestinationResponse>>>
{
    private readonly IAdminRepository _adminRepository;

    public GetTopDestinationsQueryHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task<Result<IReadOnlyList<TopDestinationResponse>>> Handle(
        GetTopDestinationsQuery request,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(request.Limit, 1, 50);
        var destinations = await _adminRepository.GetTopDestinationsAsync(
            limit,
            cancellationToken);

        return Result<IReadOnlyList<TopDestinationResponse>>.Success(
            destinations
                .Select(destination => new TopDestinationResponse(
                    destination.Name,
                    destination.TripCount))
                .ToArray());
    }
}
