namespace Safarni.Application.Features.Settings.GetMySettings;

public sealed record GetMySettingsResponse(
    Guid UserId,
    bool EmailNotificationsEnabled,
    bool PushNotificationsEnabled,
    bool ProfileIsPrivate,
    bool ShowLastSeen,
    bool ShowEmail,
    bool ShowPhoneNumber,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
