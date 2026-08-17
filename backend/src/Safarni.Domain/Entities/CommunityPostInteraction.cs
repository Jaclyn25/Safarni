using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public sealed class CommunityPostInteraction
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public CommunityPostInteractionType Type { get; set; }
    public DateTime CreatedAt { get; set; }

    public CommunityPost Post { get; set; } = null!;
    public User User { get; set; } = null!;
}
