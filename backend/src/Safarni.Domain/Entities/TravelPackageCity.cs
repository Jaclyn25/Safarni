namespace Safarni.Domain.Entities;

public sealed class TravelPackageCity
{
    public Guid Id { get; set; }
    public Guid TravelPackageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int Nights { get; set; }
    public string? Description { get; set; }
    public TravelPackage TravelPackage { get; set; } = null!;
    public ICollection<TravelPackageDay> Days { get; set; } = new List<TravelPackageDay>();
}
