namespace Safarni.Application.Features.Trips.GetNearbyAttractions;

public class NearbyAttractionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Distance { get; set; }
    public string? Website { get; set; }
    public string? Phone { get; set; }
    public string? OpeningHours { get; set; }
}
