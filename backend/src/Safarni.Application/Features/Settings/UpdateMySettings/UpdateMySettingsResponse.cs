namespace Safarni.Application.Features.Settings.UpdateMySettings;

public sealed record UpdateMySettingsResponse(
    Guid UserId,
    bool EmailNotificationsEnabled,
    bool PushNotificationsEnabled,
    bool ProfileIsPrivate,
    bool ShowLastSeen,
    bool ShowEmail,
    bool ShowPhoneNumber,
    DateTime UpdatedAt);
