using System.Globalization;
using Safarni.Application.Abstractions.Travel;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

internal sealed record ItineraryFlightPlan(
    FlightRecommendation? Outbound,
    FlightRecommendation? Return)
{
    public TimeSpan? FirstDayAvailableFrom =>
        ParseTime(Outbound?.ArrivalTime)?.Add(TimeSpan.FromMinutes(90));

    public TimeSpan? LastDayMustEndBy =>
        ParseTime(Return?.DepartureTime)?.Subtract(TimeSpan.FromMinutes(120));

    private static TimeSpan? ParseTime(string? value)
    {
        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out var parsed)
            ? parsed.TimeOfDay
            : null;
    }
}

internal static class ItineraryFlightEnricher
{
    public static async Task<ItineraryFlightPlan> ResolvePlanAsync(
        Trip trip,
        IReadOnlyList<ItineraryCityStop> cityPlan,
        string currencyCode,
        int travelerCount,
        string languageCode,
        IFlightAirportService? flightService,
        CancellationToken cancellationToken)
    {
        if (flightService?.IsConfigured != true || cityPlan.Count == 0)
        {
            return new ItineraryFlightPlan(null, null);
        }

        var originCode = TripRouteSerializer.NormalizeAirportCode(
            trip.OriginAirportCode);
        var firstCityCode = TripRouteSerializer.NormalizeAirportCode(
            cityPlan[0].AirportCode);
        var lastCityCode = TripRouteSerializer.NormalizeAirportCode(
            cityPlan[^1].AirportCode);

        // No guessing. If the traveler has not selected exact airport codes,
        // flights are intentionally omitted instead of risking unrelated routes.
        if (originCode is null || firstCityCode is null)
        {
            return new ItineraryFlightPlan(null, null);
        }

        var outboundTask = flightService.RecommendFlightAsync(
            originCode,
            firstCityCode,
            trip.StartDate.Date,
            currencyCode,
            travelerCount,
            languageCode,
            cancellationToken);

        var returnFlightTask = lastCityCode is null
            ? Task.FromResult<FlightRecommendation?>(null)
            : flightService.RecommendFlightAsync(
                lastCityCode,
                originCode,
                trip.EndDate.Date,
                currencyCode,
                travelerCount,
                languageCode,
                cancellationToken);

        await Task.WhenAll(outboundTask, returnFlightTask);
        var outbound = await outboundTask;
        var returnFlight = await returnFlightTask;

        return new ItineraryFlightPlan(
            IsExact(outbound, originCode, firstCityCode)
                ? outbound
                : null,
            IsExact(returnFlight, lastCityCode, originCode)
                ? returnFlight
                : null);
    }

    public static void AddFlights(
        Itinerary itinerary,
        Trip trip,
        IReadOnlyList<ItineraryCityStop> cityPlan,
        GenerateItineraryPreferences preferences,
        ItineraryFlightPlan flightPlan)
    {
        if (cityPlan.Count == 0)
        {
            return;
        }

        var firstDay = itinerary.Days
            .OrderBy(day => day.DayNumber)
            .FirstOrDefault();
        var lastDay = itinerary.Days
            .OrderByDescending(day => day.DayNumber)
            .FirstOrDefault();
        var isArabic = string.Equals(
            preferences.PreferredLanguage,
            "ar",
            StringComparison.OrdinalIgnoreCase);

        if (firstDay is not null && flightPlan.Outbound is { } outbound)
        {
            InsertAtStart(
                firstDay,
                CreateFlightItem(
                    firstDay.Id,
                    outbound,
                    isArabic
                        ? $"رحلة جوية إلى {cityPlan[0].City}"
                        : $"Flight to {cityPlan[0].City}",
                    isArabic));
        }

        if (lastDay is not null && flightPlan.Return is { } returnFlight)
        {
            InsertAtEnd(
                lastDay,
                CreateFlightItem(
                    lastDay.Id,
                    returnFlight,
                    isArabic
                        ? $"رحلة العودة إلى {trip.OriginCity ?? trip.OriginCountry}"
                        : $"Return flight to {trip.OriginCity ?? trip.OriginCountry}",
                    isArabic));
        }
    }

    private static ItineraryItem CreateFlightItem(
        Guid dayId,
        FlightRecommendation recommendation,
        string title,
        bool isArabic)
    {
        var stops = recommendation.StopCount == 0
            ? (isArabic ? "مباشرة" : "nonstop")
            : isArabic
                ? $"{recommendation.StopCount} توقف"
                : $"{recommendation.StopCount} stop" +
                  (recommendation.StopCount == 1 ? string.Empty : "s");

        return new ItineraryItem
        {
            Id = Guid.NewGuid(),
            ItineraryDayId = dayId,
            Title = title,
            Description =
                $"{recommendation.Airlines} · " +
                $"{recommendation.FlightNumbers} · " +
                $"{stops} · " +
                FormatDuration(
                    recommendation.TotalDurationMinutes,
                    isArabic),
            StartTime = ParseTime(recommendation.DepartureTime),
            EndTime = ParseTime(recommendation.ArrivalTime),
            Location = recommendation.ArrivalAirportName,
            Address =
                $"{recommendation.DepartureAirportName} " +
                $"({recommendation.DepartureAirportCode}) → " +
                $"{recommendation.ArrivalAirportName} " +
                $"({recommendation.ArrivalAirportCode})",
            GooglePlaceId =
                $"serp-flight:{recommendation.DepartureAirportCode}:" +
                recommendation.ArrivalAirportCode,
            PhotoUrl = recommendation.AirlineLogoUrl,
            ExternalUrl = recommendation.GoogleFlightsUrl,
            Category = ItineraryItemCategory.Transport,
            EstimatedCost = recommendation.Price,
            CostSource = "live_flight_price",
            PriceSource = "Google Flights",
            FlightDepartureAirportCode = recommendation.DepartureAirportCode,
            FlightDepartureAirportName = recommendation.DepartureAirportName,
            FlightArrivalAirportCode = recommendation.ArrivalAirportCode,
            FlightArrivalAirportName = recommendation.ArrivalAirportName,
            FlightAirlines = recommendation.Airlines,
            FlightNumbers = recommendation.FlightNumbers,
            FlightDurationMinutes = recommendation.TotalDurationMinutes,
            FlightStopCount = recommendation.StopCount
        };
    }

    private static void InsertAtStart(
        ItineraryDay day,
        ItineraryItem flight)
    {
        foreach (var item in day.Items)
        {
            item.OrderIndex++;
        }

        flight.OrderIndex = 0;
        day.Items.Add(flight);
    }

    private static void InsertAtEnd(
        ItineraryDay day,
        ItineraryItem flight)
    {
        flight.OrderIndex = day.Items.Count;
        day.Items.Add(flight);
    }

    private static bool IsExact(
        FlightRecommendation? recommendation,
        string? expectedDeparture,
        string? expectedArrival)
    {
        return recommendation is not null &&
               expectedDeparture is not null &&
               expectedArrival is not null &&
               string.Equals(
                   recommendation.DepartureAirportCode,
                   expectedDeparture,
                   StringComparison.OrdinalIgnoreCase) &&
               string.Equals(
                   recommendation.ArrivalAirportCode,
                   expectedArrival,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static TimeSpan? ParseTime(string? value)
    {
        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out var parsed)
            ? parsed.TimeOfDay
            : null;
    }

    private static string FormatDuration(
        int totalMinutes,
        bool isArabic)
    {
        if (totalMinutes <= 0)
        {
            return isArabic ? "المدة غير متاحة" : "duration unavailable";
        }

        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        return isArabic
            ? $"{hours} س {minutes} د"
            : $"{hours}h {minutes}m";
    }
}
