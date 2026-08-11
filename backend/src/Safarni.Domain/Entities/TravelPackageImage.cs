namespace Safarni.Domain.Entities;

public sealed class TravelPackageImage
{
    public Guid Id { get; set; }
    public Guid TravelPackageId { get; set; }
    public Guid? TravelPackageDayId { get; set; }
    public Guid? TravelPackageItemId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsCover { get; set; }
    public DateTime CreatedAt { get; set; }

    public TravelPackage TravelPackage { get; set; } = null!;
    public TravelPackageDay? TravelPackageDay { get; set; }
    public TravelPackageItem? TravelPackageItem { get; set; }
}
