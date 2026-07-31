namespace Safarni.Domain.Entities;

public class Itinerary
{
    public Guid Id { get; set; }

    public Guid TripId { get; set; }

    public string Preferences { get; set; } = string.Empty;

    public decimal BudgetAmount { get; set; }

    public string CurrencyCode { get; set; } = "USD";

    public int TravelerCount { get; set; } = 1;

    public ItinerarySource GeneratedBy { get; set; } =
        ItinerarySource.Ai;

    public ItineraryStatus Status { get; set; } =
        ItineraryStatus.Completed;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Trip Trip { get; set; } = null!;

    public ICollection<ItineraryDay> Days { get; set; } =
        new List<ItineraryDay>();
}