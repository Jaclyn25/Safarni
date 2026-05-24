namespace TripMindAI.Domain.Entities;

public class FlightOffer
{
    public Guid Id { get; set; }
    public Guid FlightSearchId { get; set; }
    public FlightSearch FlightSearch { get; set; } = null!;
    public string? AirlineCode { get; set; }
    public string? AirlineName { get; set; }
    public string? FlightNumber { get; set; }
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public string OriginAirportCode { get; set; } = string.Empty;
    public string DestinationAirportCode { get; set; } = string.Empty;
    public decimal PriceUsd { get; set; }
    public decimal? PriceEgp { get; set; }
    public int? StopsCount { get; set; }
    public string RawResponseJson { get; set; } = "{}";
}
