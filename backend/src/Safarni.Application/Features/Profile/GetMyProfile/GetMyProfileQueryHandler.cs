using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Profile.GetMyProfile;

public sealed class GetMyProfileQueryHandler
    : IRequestHandler<GetMyProfileQuery, Result<GetMyProfileResponse>>
{
    private readonly IUserRepository _userRepository;

    public GetMyProfileQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<GetMyProfileResponse>> Handle(
        GetMyProfileQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result<GetMyProfileResponse>.Failure(UserErrors.NotFound);
        }

        return Result<GetMyProfileResponse>.Success(
            new GetMyProfileResponse(
                user.Id,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.DateOfBirth,
                user.Country,
                user.City,
                user.Bio,
                user.ProfilePhotoUrl,
                user.CreatedAt,
                user.UpdatedAt));
    }
}