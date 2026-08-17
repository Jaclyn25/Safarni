using MediatR;
using Safarni.Application.Common.Results;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Community.Queries;

public sealed record GetCommunityFeedQuery(
    CommunityPostType? Type,
    string? Destination,
    int Take = 20) : IRequest<Result<IReadOnlyCollection<CommunityPostResponse>>>;

public sealed class GetCommunityFeedQueryHandler
    : IRequestHandler<GetCommunityFeedQuery, Result<IReadOnlyCollection<CommunityPostResponse>>>
{
    private readonly Abstractions.Persistence.ICommunityRepository _repository;
    private readonly Abstractions.Authentication.ICurrentUserService _currentUserService;

    public GetCommunityFeedQueryHandler(
        Abstractions.Persistence.ICommunityRepository repository,
        Abstractions.Authentication.ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<IReadOnlyCollection<CommunityPostResponse>>> Handle(
        GetCommunityFeedQuery request,
        CancellationToken cancellationToken)
    {
        var posts = await _repository.ListPublishedPostsAsync(
            request.Type,
            request.Destination,
            Math.Clamp(request.Take, 1, 50),
            cancellationToken);

        return Result<IReadOnlyCollection<CommunityPostResponse>>.Success(
            posts.Select(post => CommunityPostResponse.Map(post, _currentUserService.UserId)).ToArray());
    }
}
