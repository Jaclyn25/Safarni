namespace TripMindAI.Domain.Entities;

public class SavedPlace
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid PointOfInterestId { get; set; }
    public PointOfInterest PointOfInterest { get; set; } = null!;
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}
