namespace Safarni.Domain.Entities;

public class ItineraryDay
{
    public Guid Id { get; set; }

    public Guid ItineraryId { get; set; }

    public int DayNumber { get; set; }

    public DateTime Date { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public Itinerary Itinerary { get; set; } = null!;

    public ICollection<ItineraryItem> Items { get; set; } = new List<ItineraryItem>();
}
