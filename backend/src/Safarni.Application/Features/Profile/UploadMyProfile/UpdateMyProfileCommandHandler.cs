using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Profile.UpdateMyProfile;

public sealed class UpdateMyProfileCommandHandler
    : IRequestHandler<UpdateMyProfileCommand, Result<UpdateMyProfileResponse>>
{
    private readonly IUserRepository _userRepository;

    public UpdateMyProfileCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UpdateMyProfileResponse>> Handle(
        UpdateMyProfileCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result<UpdateMyProfileResponse>.Failure(UserErrors.NotFound);
        }

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : request.PhoneNumber.Trim();

        user.DateOfBirth = request.DateOfBirth;
        user.Country = string.IsNullOrWhiteSpace(request.Country)
            ? null
            : request.Country.Trim();

        user.City = string.IsNullOrWhiteSpace(request.City)
            ? null
            : request.City.Trim();

        user.Bio = string.IsNullOrWhiteSpace(request.Bio)
            ? null
            : request.Bio.Trim();

        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<UpdateMyProfileResponse>.Success(
            new UpdateMyProfileResponse(
                user.Id,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.DateOfBirth,
                user.Country,
                user.City,
                user.Bio,
                user.ProfilePhotoUrl,
                user.UpdatedAt.Value));
    }
}