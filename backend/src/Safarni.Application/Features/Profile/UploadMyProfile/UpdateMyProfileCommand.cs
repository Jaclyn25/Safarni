using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Profile.UpdateMyProfile;

public sealed record UpdateMyProfileCommand(
    Guid UserId,
    string FullName,
    string? PhoneNumber,
    DateTime? DateOfBirth,
    string? Country,
    string? City,
    string? Bio)
    : IRequest<Result<UpdateMyProfileResponse>>;






