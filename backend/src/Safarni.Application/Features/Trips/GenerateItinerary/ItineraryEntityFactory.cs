using System.Globalization;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

internal static class ItineraryEntityFactory
{
    public static Itinerary Create(
        Trip trip,
        GenerateItineraryPreferences preferences,
        decimal? budgetAmount,
        string currencyCode,
        int travelerCount,
        IEnumerable<AiItineraryDay> generatedDays,
        IReadOnlyList<ItineraryPlaceCandidate> places,
        DateTime now)
    {
        var itinerary = new Itinerary
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            Preferences =
                GenerateItineraryPreferencesSerializer.Serialize(
                    preferences with
                    {
                        QualityVersion =
                            GenerateItineraryPreferences
                                .CurrentQualityVersion
                    }),
            BudgetAmount = budgetAmount,
            CurrencyCode = currencyCode,
            TravelerCount = travelerCount,
            GeneratedBy = ItinerarySource.Ai,
            Status = ItineraryStatus.Completed,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var generatedDay in generatedDays
                     .OrderBy(day => day.DayNumber))
        {
            itinerary.Days.Add(
                CreateDay(
                    itinerary.Id,
                    generatedDay,
                    places));
        }

        return itinerary;
    }

    public static ItineraryDay CreateDay(
        Guid itineraryId,
        AiItineraryDay generatedDay,
        IReadOnlyList<ItineraryPlaceCandidate> places)
    {
        var day = new ItineraryDay
        {
            Id = Guid.NewGuid(),
            ItineraryId = itineraryId,
            DayNumber = generatedDay.DayNumber,
            Date = DateTime.ParseExact(
                generatedDay.Date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture),
            Title = generatedDay.Title.Trim(),
            Notes = generatedDay.Notes?.Trim()
        };

        foreach (var generatedItem in generatedDay.Items
                     .OrderBy(item => item.OrderIndex))
        {
            var verifiedCandidate = ItineraryPlaceCatalog.FindCandidate(
                places,
                generatedItem.PlaceReference);
            var verifiedPlace = verifiedCandidate?.Place;

            var category = ItineraryItemInput.ParseCategory(
                generatedItem.Category);

            var hasLiveAccommodationRate =
                category == ItineraryItemCategory.Accommodation &&
                verifiedPlace?.TotalRate is not null;

            day.Items.Add(new ItineraryItem
            {
                Id = Guid.NewGuid(),
                ItineraryDayId = day.Id,
                Title = generatedItem.Title.Trim(),
                Description = generatedItem.Description?.Trim(),
                StartTime = ItineraryItemInput.ParseTime(
                    generatedItem.StartTime),
                EndTime = ItineraryItemInput.ParseTime(
                    generatedItem.EndTime),

                Location = verifiedPlace?.Name
                    ?? generatedItem.Location?.Trim(),

                GooglePlaceId = verifiedPlace?.PlaceId,
                Address = verifiedPlace?.Address,
                GoogleMapsUrl = verifiedPlace?.GoogleMapsLink,
                Rating = verifiedPlace?.Rating,
                PhotoUrl = verifiedPlace?.PhotoUrl,
                Latitude = verifiedPlace?.Latitude,
                Longitude = verifiedPlace?.Longitude,
                PriceLevel = verifiedPlace?.PriceLevel,
                RatePerNight = verifiedPlace?.RatePerNight,
                StayNights = verifiedCandidate?.StayNights,
                PriceSource = verifiedPlace?.PriceSource,
                ExternalUrl = verifiedPlace?.ExternalUrl,

                Category = category,

                EstimatedCost = hasLiveAccommodationRate
                    ? verifiedPlace!.TotalRate
                    : generatedItem.EstimatedCost,
                CostSource = hasLiveAccommodationRate
                    ? "live_hotel_rate"
                    : "planning_estimate",
                OrderIndex = day.Items.Count
            });
        }

        return day;
    }
}
