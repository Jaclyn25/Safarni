using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public class TripInteraction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SnapshotVersionId { get; set; }
    public TripInteractionType Type { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }

    public SnapshotVersion SnapshotVersion { get; set; } = null!;
    public User User { get; set; } = null!;
}