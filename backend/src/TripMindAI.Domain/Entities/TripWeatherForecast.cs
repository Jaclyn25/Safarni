using TripMindAI.Domain.Enums;

namespace TripMindAI.Domain.Entities;

public class TripWeatherForecast
{
    public Guid Id { get; set; }
    public Guid TripDayId { get; set; }
    public TripDay TripDay { get; set; } = null!;
    public string LocationName { get; set; } = string.Empty;
    public DateTime ForecastDate { get; set; }
    public decimal? TemperatureCelsius { get; set; }
    public string? WeatherDescription { get; set; }
    public decimal? RainProbability { get; set; }
    public decimal? WindSpeed { get; set; }
    public ExternalProvider Provider { get; set; }
    public string? RawResponseJson { get; set; }
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
}
