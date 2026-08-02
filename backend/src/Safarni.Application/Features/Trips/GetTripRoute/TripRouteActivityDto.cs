namespace Safarni.Application.Features.Trips.GetTripRoute;

public class TripRouteActivityDto
{
    public Guid ActivityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int Order { get; set; }
    public string? Description { get; set; }
}
