namespace Safarni.Application.Weather.DTOs;

public class WeatherForecastDto
{
    public LocationDto Location { get; set; } = new();
    public List<ForecastDayDto> ForecastDays { get; set; } = new();
    public List<WeatherAlertDto> Alerts { get; set; } = new();
}

public class LocationDto
{
    public string Name { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
}

public class ForecastDayDto
{
    public DateTime Date { get; set; }
    public double MaxTempC { get; set; }
    public double MinTempC { get; set; }
    public double AvgTempC { get; set; }
    public string ConditionText { get; set; } = string.Empty;
    public string ConditionIcon { get; set; } = string.Empty;
}

public class WeatherAlertDto
{
    public string Headline { get; set; } = string.Empty;
    public string MsgType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public string Areas { get; set; } = string.Empty;
    public string Certainty { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public DateTime Effective { get; set; }
    public DateTime Expires { get; set; }
    public string Desc { get; set; } = string.Empty;
    public string Instruction { get; set; } = string.Empty;
}
