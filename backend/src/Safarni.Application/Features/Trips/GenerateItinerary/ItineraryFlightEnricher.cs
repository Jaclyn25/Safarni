using System.Globalization;
using Safarni.Application.Abstractions.Travel;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

internal static class ItineraryFlightEnricher
{
    public static async Task AddRecommendedFlightsAsync(
        Itinerary itinerary,
        Trip trip,
        IReadOnlyList<ItineraryCityStop> cityPlan,
        GenerateItineraryPreferences preferences,
        IFlightAirportService? flightService,
        CancellationToken cancellationToken)
    {
        if (flightService?.IsConfigured != true ||
            string.IsNullOrWhiteSpace(trip.OriginCountry) ||
            cityPlan.Count == 0)
        {
            return;
        }

        var recommendation =
            await flightService.RecommendFlightAsync(
                trip.OriginCountry,
                cityPlan[0].City,
                trip.StartDate.Date,
                itinerary.CurrencyCode,
                itinerary.TravelerCount,
                preferences.PreferredLanguage,
                cancellationToken);

        if (recommendation is null)
        {
            return;
        }

        var firstDay = itinerary.Days
            .OrderBy(day => day.DayNumber)
            .FirstOrDefault();

        if (firstDay is null)
        {
            return;
        }

        var isArabic = string.Equals(
            preferences.PreferredLanguage,
            "ar",
            StringComparison.OrdinalIgnoreCase);
        var stops = recommendation.StopCount == 0
            ? (isArabic ? "مباشرة" : "nonstop")
            : isArabic
                ? $"{recommendation.StopCount} توقف"
                : $"{recommendation.StopCount} stop" +
                  (recommendation.StopCount == 1 ? "" : "s");
        var duration = FormatDuration(
            recommendation.TotalDurationMinutes,
            isArabic);
        var arrivalTime = ParseTime(
            recommendation.ArrivalTime);
        var availableFrom = arrivalTime?
            .Add(TimeSpan.FromMinutes(90));

        if (availableFrom is not null)
        {
            foreach (var item in firstDay.Items
                         .Where(item =>
                             item.StartTime is not null &&
                             item.StartTime < availableFrom)
                         .ToList())
            {
                firstDay.Items.Remove(item);
            }
        }

        var remainingFirstDayItems =
            firstDay.Items
                .OrderBy(item => item.OrderIndex)
                .ToList();

        for (var index = 0;
             index < remainingFirstDayItems.Count;
             index++)
        {
            remainingFirstDayItems[index].OrderIndex =
                index + 1;
        }

        firstDay.Items.Add(
            new ItineraryItem
            {
                Id = Guid.NewGuid(),
                ItineraryDayId = firstDay.Id,
                Title = isArabic
                    ? $"رحلة جوية إلى {cityPlan[0].City}"
                    : $"Recommended flight to {cityPlan[0].City}",
                Description = isArabic
                    ? $"{recommendation.Airlines} · {recommendation.FlightNumbers} · {stops} · {duration}"
                    : $"{recommendation.Airlines} · {recommendation.FlightNumbers} · {stops} · {duration}",
                StartTime = ParseTime(
                    recommendation.DepartureTime),
                EndTime = arrivalTime,
                Location =
                    recommendation.ArrivalAirportName,
                Address =
                    $"{recommendation.DepartureAirportName} ({recommendation.DepartureAirportCode}) → " +
                    $"{recommendation.ArrivalAirportName} ({recommendation.ArrivalAirportCode})",
                GooglePlaceId =
                    $"serp-flight:{recommendation.DepartureAirportCode}:{recommendation.ArrivalAirportCode}",
                PhotoUrl =
                    recommendation.AirlineLogoUrl,
                ExternalUrl =
                    recommendation.GoogleFlightsUrl,
                Category =
                    ItineraryItemCategory.Transport,
                EstimatedCost =
                    recommendation.Price,
                CostSource =
                    "live_flight_price",
                PriceSource =
                    "Google Flights",
                OrderIndex = 0
            });

        var returnRecommendation =
            await flightService.RecommendFlightAsync(
                cityPlan[^1].City,
                trip.OriginCountry,
                trip.EndDate.Date,
                itinerary.CurrencyCode,
                itinerary.TravelerCount,
                preferences.PreferredLanguage,
                cancellationToken);

        var lastDay = itinerary.Days
            .OrderByDescending(day => day.DayNumber)
            .FirstOrDefault();

        if (returnRecommendation is null ||
            lastDay is null)
        {
            return;
        }

        var returnStops =
            returnRecommendation.StopCount == 0
                ? (isArabic ? "مباشرة" : "nonstop")
                : isArabic
                    ? $"{returnRecommendation.StopCount} توقف"
                    : $"{returnRecommendation.StopCount} stop" +
                      (returnRecommendation.StopCount == 1
                          ? ""
                          : "s");
        var returnDuration = FormatDuration(
            returnRecommendation.TotalDurationMinutes,
            isArabic);
        var returnDepartureTime = ParseTime(
            returnRecommendation.DepartureTime);

        if (returnDepartureTime is not null)
        {
            foreach (var item in lastDay.Items
                         .Where(item =>
                             item.StartTime is not null &&
                             item.StartTime >=
                                 returnDepartureTime)
                         .ToList())
            {
                lastDay.Items.Remove(item);
            }
        }

        var remainingLastDayItems =
            lastDay.Items
                .OrderBy(item => item.OrderIndex)
                .ToList();

        for (var index = 0;
             index < remainingLastDayItems.Count;
             index++)
        {
            remainingLastDayItems[index].OrderIndex =
                index;
        }

        lastDay.Items.Add(
            new ItineraryItem
            {
                Id = Guid.NewGuid(),
                ItineraryDayId = lastDay.Id,
                Title = isArabic
                    ? $"رحلة العودة إلى {trip.OriginCountry}"
                    : $"Recommended return flight to {trip.OriginCountry}",
                Description =
                    $"{returnRecommendation.Airlines} · " +
                    $"{returnRecommendation.FlightNumbers} · " +
                    $"{returnStops} · {returnDuration}",
                StartTime = returnDepartureTime,
                EndTime = ParseTime(
                    returnRecommendation.ArrivalTime),
                Location =
                    returnRecommendation.ArrivalAirportName,
                Address =
                    $"{returnRecommendation.DepartureAirportName} " +
                    $"({returnRecommendation.DepartureAirportCode}) → " +
                    $"{returnRecommendation.ArrivalAirportName} " +
                    $"({returnRecommendation.ArrivalAirportCode})",
                GooglePlaceId =
                    $"serp-flight:{returnRecommendation.DepartureAirportCode}:" +
                    returnRecommendation.ArrivalAirportCode,
                PhotoUrl =
                    returnRecommendation.AirlineLogoUrl,
                ExternalUrl =
                    returnRecommendation.GoogleFlightsUrl,
                Category =
                    ItineraryItemCategory.Transport,
                EstimatedCost =
                    returnRecommendation.Price,
                CostSource =
                    "live_flight_price",
                PriceSource =
                    "Google Flights",
                OrderIndex =
                    remainingLastDayItems.Count
            });
    }

    private static TimeSpan? ParseTime(
        string value)
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
            return isArabic
                ? "المدة غير متاحة"
                : "duration unavailable";
        }

        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;

        return isArabic
            ? $"{hours} س {minutes} د"
            : $"{hours}h {minutes}m";
    }
}
