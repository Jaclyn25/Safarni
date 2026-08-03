using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Safarni.Application.Abstractions.Integrations;
using Safarni.Application.Weather.DTOs;

namespace Safarni.Infrastructure.Integrations.Weather;

public class WeatherApiService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public WeatherApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["WeatherApi:ApiKey"] ?? throw new InvalidOperationException("WeatherApi:ApiKey is missing.");
    }

    public async Task<WeatherForecastDto> GetForecastAsync(double latitude, double longitude, int days, CancellationToken cancellationToken = default)
    {
        var url = $"https://api.weatherapi.com/v1/forecast.json?key={_apiKey}&q={latitude},{longitude}&days={days}&alerts=yes";

        var response = await _httpClient.GetFromJsonAsync<WeatherApiResponse>(url, cancellationToken);

        if (response == null)
        {
            return new WeatherForecastDto();
        }

        var dto = new WeatherForecastDto
        {
            Location = new LocationDto
            {
                Name = response.Location?.Name ?? "",
                Region = response.Location?.Region ?? "",
                Country = response.Location?.Country ?? "",
                Lat = response.Location?.Lat ?? 0,
                Lon = response.Location?.Lon ?? 0
            }
        };

        if (response.Forecast?.ForecastDay != null)
        {
            dto.ForecastDays = response.Forecast.ForecastDay.Select(f => new ForecastDayDto
            {
                Date = DateTime.TryParse(f.Date, out var d) ? d : DateTime.MinValue,
                MaxTempC = f.Day?.MaxTempC ?? 0,
                MinTempC = f.Day?.MinTempC ?? 0,
                AvgTempC = f.Day?.AvgTempC ?? 0,
                ConditionText = f.Day?.Condition?.Text ?? "",
                ConditionIcon = f.Day?.Condition?.Icon ?? ""
            }).ToList();
        }
       

        if (response.Alerts?.Alert != null)
        {
            dto.Alerts = response.Alerts.Alert.Select(a => new WeatherAlertDto
            {
                Headline = a.Headline ?? "",
                MsgType = a.MsgType ?? "",
                Severity = a.Severity ?? "",
                Urgency = a.Urgency ?? "",
                Areas = a.Areas ?? "",
                Certainty = a.Certainty ?? "",
                Event = a.Event ?? "",
                Note = a.Note ?? "",
                Effective = a.Effective,
                Expires = a.Expires,
                Desc = a.Desc ?? "",
                Instruction = a.Instruction ?? ""
            }).ToList();
        }
       
 
        return dto;
    }

    // WeatherAPI Response Models
    private class WeatherApiResponse
    {
        public LocationResponse? Location { get; set; }
        public ForecastResponse? Forecast { get; set; }
        public AlertsResponse? Alerts { get; set; }
    }

    private class LocationResponse
    {
        public string? Name { get; set; }
        public string? Region { get; set; }
        public string? Country { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    private class ForecastResponse
    {
        [JsonPropertyName("forecastday")]
        public List<ForecastDayResponse>? ForecastDay { get; set; }
    }

    private class ForecastDayResponse
    {
        public string? Date { get; set; }
        public DayResponse? Day { get; set; }
    }

    private class DayResponse
    {
        [JsonPropertyName("maxtemp_c")]
        public double MaxTempC { get; set; }

        [JsonPropertyName("mintemp_c")]
        public double MinTempC { get; set; }

        [JsonPropertyName("avgtemp_c")]
        public double AvgTempC { get; set; }

        public ConditionResponse? Condition { get; set; }
    }

    private class ConditionResponse
    {
        public string? Text { get; set; }
        public string? Icon { get; set; }
    }

    private class AlertsResponse
    {
        public List<AlertResponse>? Alert { get; set; }
    }

    private class AlertResponse
    {
        public string? Headline { get; set; }
        public string? MsgType { get; set; }
        public string? Severity { get; set; }
        public string? Urgency { get; set; }
        public string? Areas { get; set; }
        public string? Certainty { get; set; }
        public string? Event { get; set; }
        public string? Note { get; set; }
        public DateTime Effective { get; set; }
        public DateTime Expires { get; set; }
        public string? Desc { get; set; }
        public string? Instruction { get; set; }
    }
}
 