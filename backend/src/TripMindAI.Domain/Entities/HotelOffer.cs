namespace TripMindAI.Domain.Entities;

public class HotelOffer
{
    public Guid Id { get; set; }
    public Guid HotelSearchId { get; set; }
    public HotelSearch HotelSearch { get; set; } = null!;
    public string HotelName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? Rating { get; set; }
    public decimal PriceUsd { get; set; }
    public decimal? PriceEgp { get; set; }
    public string? ImageUrl { get; set; }
    public string RawResponseJson { get; set; } = "{}";
}
