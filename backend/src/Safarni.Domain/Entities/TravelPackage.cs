namespace Safarni.Domain.Entities;

public sealed class TravelPackage
{
    public Guid Id { get; set; }
    public Guid TravelAgencyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DurationDays { get; set; }
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public int AvailableSeats { get; set; }
    public string? CoverImageUrl { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public string? OfferLabel { get; set; }
    public string? Inclusions { get; set; }
    public string? Exclusions { get; set; }
    public string? ImportantNotes { get; set; }
    public string? CancellationPolicy { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public TravelAgency TravelAgency { get; set; } = null!;
    public ICollection<TravelPackageCity> Cities { get; set; } = new List<TravelPackageCity>();
    public ICollection<TravelPackageDay> Days { get; set; } = new List<TravelPackageDay>();
    public ICollection<TravelPackageImage> Images { get; set; } = new List<TravelPackageImage>();
    public ICollection<PackageBookingRequest> BookingRequests { get; set; } = new List<PackageBookingRequest>();
}
