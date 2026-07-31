namespace Safarni.Domain.Entities;

public class Trip
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public string OriginCountry { get; set; } = string.Empty;

    public string SelectedCitiesJson { get; set; } = "[]";

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;

    public ICollection<Itinerary> Itineraries { get; set; } = new List<Itinerary>();
}
