namespace Safarni.Domain.Entities;

public class TripRating
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SnapshotVersionId { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public SnapshotVersion SnapshotVersion { get; set; } = null!;
    public User User { get; set; } = null!;
}