using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public sealed class UserNotification
{
    public Guid Id { get; set; }
    public Guid RecipientUserId { get; set; }
    public MarketplaceNotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public User RecipientUser { get; set; } = null!;
}
