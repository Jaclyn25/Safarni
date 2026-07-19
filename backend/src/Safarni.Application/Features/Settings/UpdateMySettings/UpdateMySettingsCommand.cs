using MediatR;
using Safarni.Application.Common.Results;

namespace Safarni.Application.Features.Settings.UpdateMySettings;

public sealed record UpdateMySettingsCommand(
    Guid UserId,
    bool EmailNotificationsEnabled,
    bool PushNotificationsEnabled,
    bool ProfileIsPrivate,
    bool ShowLastSeen,
    bool ShowEmail,
    bool ShowPhoneNumber) : IRequest<Result<UpdateMySettingsResponse>>;
