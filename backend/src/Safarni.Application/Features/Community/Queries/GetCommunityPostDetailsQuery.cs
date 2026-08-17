using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Community.Queries;

public sealed record GetCommunityPostDetailsQuery(Guid PostId)
    : IRequest<Result<CommunityPostResponse>>;

public sealed class GetCommunityPostDetailsQueryHandler
    : IRequestHandler<GetCommunityPostDetailsQuery, Result<CommunityPostResponse>>
{
    private readonly Abstractions.Persistence.ICommunityRepository _repository;
    private readonly Abstractions.Authentication.ICurrentUserService _currentUserService;

    public GetCommunityPostDetailsQueryHandler(
        Abstractions.Persistence.ICommunityRepository repository,
        Abstractions.Authentication.ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CommunityPostResponse>> Handle(
        GetCommunityPostDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var post = await _repository.GetPostAsync(request.PostId, cancellationToken);

        if (post == null || post.Status != Domain.Enums.CommunityPostStatus.Published)
        {
            return Result<CommunityPostResponse>.Failure(
                new Common.Errors.Error("Community.PostNotFound", "Community post not found.", Common.Errors.ErrorType.NotFound));
        }

        return Result<CommunityPostResponse>.Success(
            CommunityPostResponse.Map(post, _currentUserService.UserId, includeComments: true));
    }
}
