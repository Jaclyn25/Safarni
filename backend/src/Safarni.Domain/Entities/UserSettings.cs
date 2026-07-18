namespace Safarni.Domain.Entities;

public class UserSettings
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public bool EmailNotificationsEnabled { get; set; }

    public bool PushNotificationsEnabled { get; set; }

    public bool ProfileIsPrivate { get; set; }

    public bool ShowLastSeen { get; set; }

    public bool ShowEmail { get; set; }

    public bool ShowPhoneNumber { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
