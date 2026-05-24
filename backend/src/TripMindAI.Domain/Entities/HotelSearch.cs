using TripMindAI.Domain.Enums;

namespace TripMindAI.Domain.Entities;

public class HotelSearch
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid? TripPlanId { get; set; }
    public TripPlan? TripPlan { get; set; }
    public string DestinationCity { get; set; } = string.Empty;
    public string DestinationCountry { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int Adults { get; set; }
    public int Rooms { get; set; }
    public ExternalProvider Provider { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<HotelOffer> Offers { get; set; } = new List<HotelOffer>();
}
