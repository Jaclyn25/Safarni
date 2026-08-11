using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public sealed class TravelPackageItem
{
    public Guid Id { get; set; }
    public Guid TravelPackageDayId { get; set; }
    public TravelPackageItemType Type { get; set; }
    public TravelPackageItemSource Source { get; set; } = TravelPackageItemSource.Manual;
    public int OrderIndex { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Location is intentionally optional. An agency can describe a walking
    // program or free-form experience without pinning it to a provider result.
    public string? LocationName { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? MapUrl { get; set; }
    public string? Provider { get; set; }
    public string? ProviderPlaceId { get; set; }
    public string? ProviderPhotoUrl { get; set; }

    public string? WebsiteUrl { get; set; }

    // Category-specific optional details.
    public string? Cuisine { get; set; }
    public string? MealType { get; set; }
    public bool? ReservationRequired { get; set; }
    public string? AccommodationType { get; set; }
    public int? StarRating { get; set; }
    public string? RoomType { get; set; }
    public string? BoardBasis { get; set; }
    public string? TransportMode { get; set; }
    public string? FromLocation { get; set; }
    public string? ToLocation { get; set; }
    public string? MeetingPoint { get; set; }
    public string? DifficultyLevel { get; set; }
    public string? AdditionalDetails { get; set; }

    public TravelPackageDay TravelPackageDay { get; set; } = null!;
    public ICollection<TravelPackageImage> Images { get; set; } = new List<TravelPackageImage>();
}
