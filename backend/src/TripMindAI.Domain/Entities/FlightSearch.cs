using TripMindAI.Domain.Enums;

namespace TripMindAI.Domain.Entities;

public class FlightSearch
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid? TripPlanId { get; set; }
    public TripPlan? TripPlan { get; set; }
    public string OriginAirportCode { get; set; } = string.Empty;
    public string DestinationAirportCode { get; set; } = string.Empty;
    public DateTime DepartureDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public ExternalProvider Provider { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FlightOffer> Offers { get; set; } = new List<FlightOffer>();
}
