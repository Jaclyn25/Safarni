using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Settings.UpdateMySettings;

public sealed class UpdateMySettingsCommandHandler : IRequestHandler<UpdateMySettingsCommand, Result<UpdateMySettingsResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserSettingsRepository _userSettingsRepository;

    public UpdateMySettingsCommandHandler(IUserRepository userRepository, IUserSettingsRepository userSettingsRepository)
    {
        _userRepository = userRepository;
        _userSettingsRepository = userSettingsRepository;
    }

    public async Task<Result<UpdateMySettingsResponse>> Handle(UpdateMySettingsCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result<UpdateMySettingsResponse>.Failure(UserErrors.NotFound);
        }

        var settings = await _userSettingsRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var isNewSettings = settings is null;

        settings ??= new UserSettings
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            CreatedAt = DateTime.UtcNow
        };

        settings.EmailNotificationsEnabled = request.EmailNotificationsEnabled;
        settings.PushNotificationsEnabled = request.PushNotificationsEnabled;
        settings.ProfileIsPrivate = request.ProfileIsPrivate;
        settings.ShowLastSeen = request.ShowLastSeen;
        settings.ShowEmail = request.ShowEmail;
        settings.ShowPhoneNumber = request.ShowPhoneNumber;
        settings.UpdatedAt = DateTime.UtcNow;

        if (isNewSettings)
        {
            await _userSettingsRepository.AddAsync(settings, cancellationToken);
        }
        else
        {
            await _userSettingsRepository.UpdateAsync(settings, cancellationToken);
        }

        return Result<UpdateMySettingsResponse>.Success(new UpdateMySettingsResponse(
            settings.UserId,
            settings.EmailNotificationsEnabled,
            settings.PushNotificationsEnabled,
            settings.ProfileIsPrivate,
            settings.ShowLastSeen,
            settings.ShowEmail,
            settings.ShowPhoneNumber,
            settings.UpdatedAt!.Value));
    }
}
