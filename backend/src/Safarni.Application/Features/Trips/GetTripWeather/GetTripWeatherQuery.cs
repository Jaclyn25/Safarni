using Safarni.Application.Common.Results;
using Safarni.Application.Weather.DTOs;
using MediatR;

namespace Safarni.Application.Features.Trips.GetTripWeather;

public sealed record GetTripWeatherQuery(Guid TripId) : IRequest<Result<IReadOnlyList<ItineraryDayWeatherResponse>>>;

public sealed record ItineraryDayWeatherResponse(
    Guid DayId,
    DateTime Date,
    WeatherForecastDto Weather
);
