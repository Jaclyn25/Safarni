using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public class Trip
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public string OriginCountry { get; set; } = string.Empty;

    public string? OriginCountryCode { get; set; }

    public string? OriginCity { get; set; }

    public string? OriginAirportCode { get; set; }

    public string? OriginAirportName { get; set; }

    public TripStatus Status { get; set; } = TripStatus.Draft;

    public bool IsPublished { get; set; }

    public DateTime? PublishedAt { get; set; }

    public TripVisibility Visibility { get; set; } = TripVisibility.Private;

    /// <summary>
    /// Legacy compatibility copy. New code reads ordered <see cref="Cities"/> rows.
    /// It is kept during the transition so old records and old clients remain readable.
    /// </summary>
    public string SelectedCitiesJson { get; set; } = "[]";

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public User User { get; set; } = null!;

    public ICollection<TripCity> Cities { get; set; } = new List<TripCity>();

    public ICollection<Itinerary> Itineraries { get; set; } = new List<Itinerary>();

    public ICollection<ItineraryGenerationJob> GenerationJobs { get; set; } =
        new List<ItineraryGenerationJob>();

    public PublicTripSnapshot? PublicSnapshot { get; set; }
}