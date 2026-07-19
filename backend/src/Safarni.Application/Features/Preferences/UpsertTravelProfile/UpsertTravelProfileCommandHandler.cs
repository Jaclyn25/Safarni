using MediatR;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Preferences.UpsertTravelProfile;

public sealed class UpsertTravelProfileCommandHandler : IRequestHandler<UpsertTravelProfileCommand, Result<TravelProfileResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITravelProfileRepository _travelProfileRepository;

    public UpsertTravelProfileCommandHandler(
        ICurrentUserService currentUserService,
        ITravelProfileRepository travelProfileRepository)
    {
        _currentUserService = currentUserService;
        _travelProfileRepository = travelProfileRepository;
    }

    public async Task<Result<TravelProfileResponse>> Handle(UpsertTravelProfileCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
        {
            return Result<TravelProfileResponse>.Failure(AuthenticationErrors.AuthenticationRequired);
        }

        var updatedAt = DateTime.UtcNow;
        var preference = await _travelProfileRepository.GetTravelPreferenceByUserIdAsync(userId, cancellationToken);
        var passportInfo = await _travelProfileRepository.GetPassportInfoByUserIdAsync(userId, cancellationToken);

        if (preference is null)
        {
            preference = new TravelPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId
            };
            await _travelProfileRepository.AddTravelPreferenceAsync(preference, cancellationToken);
        }

        if (passportInfo is null)
        {
            passportInfo = new PassportInfo
            {
                Id = Guid.NewGuid(),
                UserId = userId
            };
            await _travelProfileRepository.AddPassportInfoAsync(passportInfo, cancellationToken);
        }

        preference.PreferredDestinationCountry = request.PreferredDestinationCountry.Trim();
        preference.TravelPurpose = request.TravelPurpose.Trim();
        preference.TravelStyle = request.TravelStyle.Trim();
        preference.MinBudget = request.MinBudget;
        preference.MaxBudget = request.MaxBudget;
        preference.UpdatedAt = updatedAt;

        passportInfo.PassportNumber = request.PassportNumber.Trim();
        passportInfo.Nationality = request.Nationality.Trim();
        passportInfo.ExpiryDate = request.PassportExpiryDate;
        passportInfo.UpdatedAt = updatedAt;

        await _travelProfileRepository.SaveAsync(cancellationToken);

        return Result<TravelProfileResponse>.Success(MapToResponse(userId, preference, passportInfo, updatedAt));
    }

    private static TravelProfileResponse MapToResponse(
        Guid userId,
        TravelPreference preference,
        PassportInfo passportInfo,
        DateTime updatedAt)
    {
        return new TravelProfileResponse(
            userId,
            preference.PreferredDestinationCountry,
            preference.TravelPurpose,
            preference.TravelStyle,
            preference.MinBudget,
            preference.MaxBudget,
            passportInfo.PassportNumber,
            passportInfo.Nationality,
            passportInfo.ExpiryDate,
            updatedAt);
    }
}
