namespace Safarni.Domain.Entities;

public class TripCity
{
    public Guid Id { get; set; }

    public Guid TripId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Days { get; set; }

    public int OrderIndex { get; set; }

    /// <summary>
    /// Exact IATA airport selected for this city, or the explicitly selected
    /// nearest airport when the city has no commercial airport (for example SSH for Dahab).
    /// Null means flights must not be generated for this stop.
    /// </summary>
    public string? AirportCode { get; set; }

    public string? AirportName { get; set; }

    public Trip Trip { get; set; } = null!;
}
