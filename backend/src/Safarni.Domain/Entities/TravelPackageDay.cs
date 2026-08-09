namespace Safarni.Domain.Entities;

public sealed class TravelPackageDay
{
    public Guid Id { get; set; }
    public Guid TravelPackageId { get; set; }
    public int DayNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Activities { get; set; }
    public TravelPackage TravelPackage { get; set; } = null!;
}
