using Safarni.Application.Abstractions.Integrations;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Errors;
using Safarni.Application.Common.Results;
using MediatR;

namespace Safarni.Application.Features.Trips.GetTripWeather;

internal sealed class GetTripWeatherQueryHandler
    : IRequestHandler<GetTripWeatherQuery, Result<IReadOnlyList<ItineraryDayWeatherResponse>>>
{
    private readonly ITripRepository _tripRepository;
    private readonly IItineraryRepository _itineraryRepository;
    private readonly IWeatherService _weatherService;

    public GetTripWeatherQueryHandler(
        ITripRepository tripRepository,
        IItineraryRepository itineraryRepository,
        IWeatherService weatherService)
    {
        _tripRepository = tripRepository;
        _itineraryRepository = itineraryRepository;
        _weatherService = weatherService;
    }

    public async Task<Result<IReadOnlyList<ItineraryDayWeatherResponse>>> Handle(
        GetTripWeatherQuery request,
        CancellationToken cancellationToken)
    {
        var trip = await _tripRepository.GetByIdAsync(request.TripId, cancellationToken);
        if (trip == null)
        {
            return Result<IReadOnlyList<ItineraryDayWeatherResponse>>.Failure(TripErrors.NotFound);
        }

        var itinerary = await _itineraryRepository.GetActiveByTripIdAsync(request.TripId, cancellationToken);
        if (itinerary == null)
        {
            return Result<IReadOnlyList<ItineraryDayWeatherResponse>>.Failure(
                new Error("Itinerary.NotFound", "No active itinerary found for this trip. Cannot determine weather by location without an itinerary.", ErrorType.NotFound));
        }

        var result = new List<ItineraryDayWeatherResponse>();

        foreach (var day in itinerary.Days.OrderBy(d => d.Date))
        {
            // Find the first item with valid coordinates to use as the location for the day's weather
            var firstItemWithLocation = day.Items
                .OrderBy(i => i.OrderIndex)
                .FirstOrDefault(i => i.Latitude.HasValue && i.Longitude.HasValue);

            if (firstItemWithLocation != null && firstItemWithLocation.Latitude.HasValue && firstItemWithLocation.Longitude.HasValue)
            {
                // We only need 1 day of forecast since we are querying for a specific location
                // However, weather api usually returns a forecast from today up to 'days'
                // We will request 14 days (max typical for free tiers) and filter on the frontend,
                // or we can just fetch and return the whole block.
                var weather = await _weatherService.GetForecastAsync(
                    firstItemWithLocation.Latitude.Value,
                    firstItemWithLocation.Longitude.Value,
                    14,
                    cancellationToken);
                
                result.Add(new ItineraryDayWeatherResponse(day.Id, day.Date, weather));
            }
        }

        return Result<IReadOnlyList<ItineraryDayWeatherResponse>>.Success(result);
    }
}
