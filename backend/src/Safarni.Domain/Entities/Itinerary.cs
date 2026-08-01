namespace Safarni.Domain.Entities;

public class Itinerary
{
    public Guid Id { get; set; }

    public Guid TripId { get; set; }

    public string Preferences { get; set; } = string.Empty;

    /// <summary>
    /// User-provided trip budget. Null means the traveler did not provide a budget
    /// and the generated costs are planning estimates rather than a budget guarantee.
    /// </summary>
    public decimal? BudgetAmount { get; set; }

    public string CurrencyCode { get; set; } = "USD";

    public int TravelerCount { get; set; } = 1;

    public ItinerarySource GeneratedBy { get; set; } = ItinerarySource.Ai;

    public ItineraryStatus Status { get; set; } = ItineraryStatus.Completed;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public Trip Trip { get; set; } = null!;

    public ICollection<ItineraryDay> Days { get; set; } =
        new List<ItineraryDay>();
}
