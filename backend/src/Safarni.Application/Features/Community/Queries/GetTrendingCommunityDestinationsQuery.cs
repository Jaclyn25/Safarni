using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Community.Queries;

public sealed record GetTrendingCommunityDestinationsQuery(int Take = 5)
    : IRequest<Result<IReadOnlyCollection<CommunityDestinationTrendResponse>>>;

public sealed class GetTrendingCommunityDestinationsQueryHandler
    : IRequestHandler<GetTrendingCommunityDestinationsQuery, Result<IReadOnlyCollection<CommunityDestinationTrendResponse>>>
{
    private readonly ICommunityRepository _repository;

    public GetTrendingCommunityDestinationsQueryHandler(ICommunityRepository repository) => _repository = repository;

    public async Task<Result<IReadOnlyCollection<CommunityDestinationTrendResponse>>> Handle(GetTrendingCommunityDestinationsQuery request, CancellationToken cancellationToken)
    {
        var destinations = await _repository.ListTrendingDestinationsAsync(request.Take, cancellationToken);
        return Result<IReadOnlyCollection<CommunityDestinationTrendResponse>>.Success(
            destinations.Select(item => new CommunityDestinationTrendResponse(item.Destination, item.PostsCount, item.EngagementCount)).ToArray());
    }
}
