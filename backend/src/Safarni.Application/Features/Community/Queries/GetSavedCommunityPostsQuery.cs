using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Community.Queries;

public sealed record GetSavedCommunityPostsQuery(int Take = 10)
    : IRequest<Result<IReadOnlyCollection<CommunityPostResponse>>>;

public sealed class GetSavedCommunityPostsQueryHandler
    : IRequestHandler<GetSavedCommunityPostsQuery, Result<IReadOnlyCollection<CommunityPostResponse>>>
{
    private readonly ICommunityRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetSavedCommunityPostsQueryHandler(ICommunityRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyCollection<CommunityPostResponse>>> Handle(GetSavedCommunityPostsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
            return Result<IReadOnlyCollection<CommunityPostResponse>>.Failure(new Error("Community.Unauthorized", "Sign in to view saved posts.", ErrorType.Unauthorized));

        var posts = await _repository.ListSavedPostsAsync(userId, request.Take, cancellationToken);
        return Result<IReadOnlyCollection<CommunityPostResponse>>.Success(posts.Select(post => CommunityPostResponse.Map(post, userId)).ToArray());
    }
}
