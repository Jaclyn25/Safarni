using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Settings.GetMySettings;

public sealed class GetMySettingsQueryHandler : IRequestHandler<GetMySettingsQuery, Result<GetMySettingsResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserSettingsRepository _userSettingsRepository;

    public GetMySettingsQueryHandler(IUserRepository userRepository, IUserSettingsRepository userSettingsRepository)
    {
        _userRepository = userRepository;
        _userSettingsRepository = userSettingsRepository;
    }

    public async Task<Result<GetMySettingsResponse>> Handle(GetMySettingsQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result<GetMySettingsResponse>.Failure(UserErrors.NotFound);
        }

        var settings = await _userSettingsRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (settings is null)
        {
            settings = CreateDefaultSettings(request.UserId);
            await _userSettingsRepository.AddAsync(settings, cancellationToken);
        }

        return Result<GetMySettingsResponse>.Success(Map(settings));
    }

    private static UserSettings CreateDefaultSettings(Guid userId)
    {
        return new UserSettings
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EmailNotificationsEnabled = true,
            PushNotificationsEnabled = true,
            ProfileIsPrivate = false,
            ShowLastSeen = true,
            ShowEmail = false,
            ShowPhoneNumber = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static GetMySettingsResponse Map(UserSettings settings)
    {
        return new GetMySettingsResponse(
            settings.UserId,
            settings.EmailNotificationsEnabled,
            settings.PushNotificationsEnabled,
            settings.ProfileIsPrivate,
            settings.ShowLastSeen,
            settings.ShowEmail,
            settings.ShowPhoneNumber,
            settings.CreatedAt,
            settings.UpdatedAt);
    }
}
