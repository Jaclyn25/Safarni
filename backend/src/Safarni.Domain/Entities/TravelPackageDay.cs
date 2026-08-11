namespace Safarni.Domain.Entities;

public sealed class TravelPackageDay
{
    public Guid Id { get; set; }
    public Guid TravelPackageId { get; set; }
    public Guid? TravelPackageCityId { get; set; }
    public int DayNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TravelPackage TravelPackage { get; set; } = null!;
    public TravelPackageCity? TravelPackageCity { get; set; }
    public ICollection<TravelPackageItem> Items { get; set; } = new List<TravelPackageItem>();
    public ICollection<TravelPackageImage> Images { get; set; } = new List<TravelPackageImage>();
}
