namespace TripMindAI.Domain.Entities;

public class TripDay
{
    public Guid Id { get; set; }
    public Guid TripPlanId { get; set; }
    public TripPlan TripPlan { get; set; } = null!;
    public int DayNumber { get; set; }
    public DateTime Date { get; set; }
    public string? Title { get; set; }

    public ICollection<TripActivity> Activities { get; set; } = new List<TripActivity>();
    public TripWeatherForecast? WeatherForecast { get; set; }
}
