using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Preferences.GetTravelProfile;

public sealed class GetTravelProfileQueryHandler : IRequestHandler<GetTravelProfileQuery, Result<TravelProfileResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITravelProfileRepository _travelProfileRepository;

    public GetTravelProfileQueryHandler(
        ICurrentUserService currentUserService,
        ITravelProfileRepository travelProfileRepository)
    {
        _currentUserService = currentUserService;
        _travelProfileRepository = travelProfileRepository;
    }

    public async Task<Result<TravelProfileResponse>> Handle(GetTravelProfileQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Result<TravelProfileResponse>.Failure(AuthenticationErrors.AuthenticationRequired);
        }

        var preference = await _travelProfileRepository.GetTravelPreferenceByUserIdAsync(userId, cancellationToken);
        var passportInfo = await _travelProfileRepository.GetPassportInfoByUserIdAsync(userId, cancellationToken);

        if (preference is null || passportInfo is null)
        {
            return Result<TravelProfileResponse>.Failure(TravelProfileErrors.NotFound);
        }

        var updatedAt = preference.UpdatedAt > passportInfo.UpdatedAt ? preference.UpdatedAt : passportInfo.UpdatedAt;

        return Result<TravelProfileResponse>.Success(new TravelProfileResponse(
            userId,
            preference.PreferredDestinationCountry,
            preference.TravelPurpose,
            preference.TravelStyle,
            preference.MinBudget,
            preference.MaxBudget,
            passportInfo.PassportNumber,
            passportInfo.Nationality,
            passportInfo.ExpiryDate,
            updatedAt));
    }
}
