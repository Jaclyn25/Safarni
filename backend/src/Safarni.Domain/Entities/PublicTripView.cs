using System;

namespace Safarni.Domain.Entities;

public class PublicTripView
{
    public Guid Id { get; set; }
    public Guid SnapshotVersionId { get; set; }
    public Guid? UserId { get; set; }
    public string? SessionId { get; set; }
    public DateTime ViewedAt { get; set; }

    public SnapshotVersion SnapshotVersion { get; set; } = null!;
    public User? User { get; set; }
}
