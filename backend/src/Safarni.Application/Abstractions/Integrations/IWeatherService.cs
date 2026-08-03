using Safarni.Application.Weather.DTOs;

namespace Safarni.Application.Abstractions.Integrations;

public interface IWeatherService
{
    Task<WeatherForecastDto> GetForecastAsync(double latitude, double longitude, int days, CancellationToken cancellationToken = default);
}
