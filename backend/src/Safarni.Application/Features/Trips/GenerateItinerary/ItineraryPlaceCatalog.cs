using System.Globalization;
using System.Text;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using Safarni.Application.Abstractions.Travel;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

internal sealed record ItineraryPlaceCandidate(
    string Reference,
    PlaceDetails Place,
    ItineraryPlacePurpose Purpose,
    string City);

internal enum ItineraryPlacePurpose
{
    Activity,
    Meal,
    Accommodation
}

internal sealed record ItineraryPlaceSearch(
    string Query,
    ItineraryPlacePurpose Purpose);

internal static class ItineraryPlaceCatalog
{
    public static async Task<IReadOnlyList<ItineraryPlaceCandidate>> LoadAsync(
        IGooglePlacesService googlePlacesService,
        IItineraryTravelDataService? travelDataService,
        Trip trip,
        IReadOnlyList<ItineraryCityStop> cityPlan,
        GenerateItineraryPreferences preferences,
        string currencyCode,
        int travelerCount,
        CancellationToken cancellationToken)
    {
        var places =
            new List<(
                PlaceDetails Place,
                ItineraryPlacePurpose Purpose,
                string City)>();

        foreach (var cityStop in cityPlan)
        {
            var cityAliases =
                await ResolveDestinationAliasesAsync(
                    googlePlacesService,
                    cityStop.City,
                    cancellationToken);

            var queries = BuildSearchQueries(
                cityStop.City,
                cityStop.DayCount,
                preferences);

            foreach (var search in queries)
            {
                var results = await SearchAsync(
                googlePlacesService,
                travelDataService,
                search,
                trip,
                cityStop,
                preferences,
                currencyCode,
                travelerCount,
                cancellationToken);

                places.AddRange(
                    results
                        .Where(IsUsablePlace)
                        .Where(place =>
                            IsInDestination(
                                place,
                                cityAliases))
                        .Where(place =>
                            MatchesPurpose(
                                place,
                                search.Purpose))
                        .OrderByDescending(
                            place => place.Rating ?? 0)
                        .ThenByDescending(
                            place =>
                                place.UserRatingsTotal ?? 0)
                        .Take(
                            GetCandidateLimit(
                                search.Purpose,
                                cityStop.DayCount,
                                preferences.Pace))
                        .Select(place =>
                            (
                                place,
                                search.Purpose,
                                cityStop.City)));
            }
        }

        var uniquePlaces = places
            .GroupBy(
                candidate =>
                    $"{candidate.City}:" +
                    candidate.Place.PlaceId,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        var activitiesPerDay =
            preferences.Pace.Trim().ToLowerInvariant() switch
            {
                "active" => 3,
                "relaxed" => 1,
                _ => 2
            };

        var selectedPlaces = cityPlan
            .SelectMany(cityStop =>
                uniquePlaces
                    .Where(candidate =>
                        string.Equals(
                            candidate.City,
                            cityStop.City,
                            StringComparison.OrdinalIgnoreCase) &&
                        candidate.Purpose ==
                        ItineraryPlacePurpose.Activity)
                    .Take(
                        cityStop.DayCount *
                        activitiesPerDay)
                    .Concat(
                        uniquePlaces
                            .Where(candidate =>
                                string.Equals(
                                    candidate.City,
                                    cityStop.City,
                                    StringComparison.OrdinalIgnoreCase) &&
                                candidate.Purpose ==
                                ItineraryPlacePurpose.Meal)
                            .Take(cityStop.DayCount * 3))
                    .Concat(
                        uniquePlaces
                            .Where(candidate =>
                                string.Equals(
                                    candidate.City,
                                    cityStop.City,
                                    StringComparison.OrdinalIgnoreCase) &&
                                candidate.Purpose ==
                                ItineraryPlacePurpose.Accommodation)
                            .OrderBy(candidate =>
                                candidate.Place.TotalRate
                                ?? decimal.MaxValue)
                            .Take(3)))
            .Select((candidate, index) =>
                new ItineraryPlaceCandidate(
                    $"P{index + 1}",
                    candidate.Place,
                    candidate.Purpose,
                    candidate.City))
            .ToList();

        return selectedPlaces;
    }

    private static async Task<IReadOnlyList<PlaceDetails>>
        SearchAsync(
            IGooglePlacesService googlePlacesService,
            IItineraryTravelDataService? travelDataService,
            ItineraryPlaceSearch search,
            Trip trip,
            ItineraryCityStop cityStop,
            GenerateItineraryPreferences preferences,
            string currencyCode,
            int travelerCount,
            CancellationToken cancellationToken)
    {
        IReadOnlyList<PlaceDetails> results = [];

        if (travelDataService?.IsConfigured == true)
        {
            if (search.Purpose ==
                ItineraryPlacePurpose.Accommodation)
            {
                results =
                    await travelDataService.SearchHotelsAsync(
                        cityStop.City,
                        preferences.AccommodationStyle,
                        cityStop.GetStartDate(trip),
                        GetHotelCheckOutDate(
                            trip,
                            cityStop),
                        travelerCount,
                        currencyCode,
                        preferences.PreferredLanguage,
                        cancellationToken);

                if (results.Count == 0)
                {
                    results =
                        await travelDataService.SearchLocalAsync(
                            search.Query,
                            cityStop.City,
                            preferences.PreferredLanguage,
                            cancellationToken);
                }
            }
            else
            {
                results =
                    await travelDataService.SearchLocalAsync(
                        search.Query,
                        cityStop.City,
                        preferences.PreferredLanguage,
                        cancellationToken);
            }
        }

        if (results.Count > 0)
        {
            return results;
        }

        return await googlePlacesService.TextSearchAsync(
            search.Query,
            cancellationToken,
            "en");
    }

    private static DateTime GetHotelCheckOutDate(
        Trip trip,
        ItineraryCityStop cityStop)
    {
        // Trip dates are inclusive in Safarni. A city allocated three
        // itinerary days therefore owns a three-night hotel stay.
        return cityStop.GetEndDate(trip).AddDays(1);
    }

    private static IReadOnlyList<ItineraryPlaceSearch>
        BuildSearchQueries(
        string destination,
        int dayCount,
        GenerateItineraryPreferences preferences)
    {
        var activityPhrases = preferences.Interests
            .Where(interest => !IsFoodInterest(interest))
            .Select(BuildInterestSearchPhrase)
            .Where(phrase =>
                !string.IsNullOrWhiteSpace(phrase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var activitySearch = activityPhrases.Count == 0
            ? "top rated attractions and local experiences"
            : "top rated attractions, local experiences, and " +
              string.Join(" and ", activityPhrases);

        var queries = new List<ItineraryPlaceSearch>
        {
            new(
                $"{activitySearch} in {destination}",
                ItineraryPlacePurpose.Activity)
        };

        if (GetActivitiesPerDay(preferences.Pace) *
            Math.Max(1, dayCount) > 20)
        {
            queries.Add(
                new ItineraryPlaceSearch(
                    $"must see landmarks and unique experiences in {destination}",
                    ItineraryPlacePurpose.Activity));
        }

        if (!string.Equals(
                preferences.AccommodationStyle,
                "already_booked",
                StringComparison.OrdinalIgnoreCase))
        {
            queries.Add(
                new ItineraryPlaceSearch(
                    $"{BuildAccommodationSearchPhrase(preferences.AccommodationStyle)} in {destination}",
                    ItineraryPlacePurpose.Accommodation));
        }

        return queries
            .DistinctBy(search =>
                $"{search.Purpose}:{search.Query}",
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int GetCandidateLimit(
        ItineraryPlacePurpose purpose,
        int dayCount,
        string pace)
    {
        var effectiveDayCount = Math.Max(1, dayCount);

        return purpose switch
        {
            ItineraryPlacePurpose.Activity =>
                Math.Min(
                    20,
                    effectiveDayCount *
                    GetActivitiesPerDay(pace)),
            ItineraryPlacePurpose.Meal =>
                Math.Min(20, effectiveDayCount * 3),
            ItineraryPlacePurpose.Accommodation => 12,
            _ => 20
        };
    }

    private static int GetActivitiesPerDay(string pace)
    {
        return pace.Trim().ToLowerInvariant() switch
        {
            "active" => 3,
            "relaxed" => 1,
            _ => 2
        };
    }

    private static bool IsFoodInterest(string interest)
    {
        return string.Equals(
            interest.Trim(),
            "food",
            StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildInterestSearchPhrase(
        string interest)
    {
        return interest.Trim().ToLowerInvariant() switch
        {
            "adventure" =>
                "adventure and outdoor activities",
            "nature" =>
                "parks nature reserves and scenic places",
            "culture" =>
                "cultural attractions museums and historic places",
            "food" =>
                "local food experiences and restaurants",
            "shopping" =>
                "popular markets and shopping districts",
            "nightlife" =>
                "nightlife and evening entertainment",
            "relaxation" =>
                "relaxing spas beaches and quiet places",
            "family" =>
                "family friendly attractions and activities",
            _ =>
                interest.Replace('_', ' ').Trim()
        };
    }

    private static string BuildAccommodationSearchPhrase(
        string accommodationStyle)
    {
        return accommodationStyle.Trim().ToLowerInvariant() switch
        {
            "budget" => "best budget hotels",
            "mid_range" => "best mid range hotels",
            "luxury" => "best luxury hotels",
            "boutique" => "best boutique hotels",
            "resort" => "best resorts",
            _ => "recommended hotels"
        };
    }

    public static PlaceDetails? FindPlace(
        IReadOnlyList<ItineraryPlaceCandidate> places,
        string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        return places
            .FirstOrDefault(candidate =>
                string.Equals(
                    candidate.Reference,
                    reference.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            ?.Place;
    }

    public static ItineraryPlaceCandidate? FindCandidate(
        IReadOnlyList<ItineraryPlaceCandidate> places,
        string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        return places.FirstOrDefault(candidate =>
            string.Equals(
                candidate.Reference,
                reference.Trim(),
                StringComparison.OrdinalIgnoreCase));
    }

    public static string BuildPromptCatalog(
    IReadOnlyList<ItineraryPlaceCandidate> places)
    {
        var builder = new StringBuilder();

        foreach (var candidate in places)
        {
            var place = candidate.Place;

            var rating = place.Rating?.ToString(
                "0.0",
                CultureInfo.InvariantCulture) ?? "N/A";

            var types = place.Types.Count > 0
                ? place.Types[0]
                : "N/A";

            var priceDetails = BuildPriceDetails(place);

            builder.AppendLine(
                $"{candidate.Reference}|" +
                $"purpose:{FormatPurpose(candidate.Purpose)}|" +
                $"city:{Clean(candidate.City)}|" +
                $"name:{Clean(place.Name)}|" +
                $"address:{Clean(place.Address, 100)}|" +
                $"rating:{rating}|" +
                $"types:{Clean(types)}" +
                priceDetails);
        }

        return builder.ToString();
    }

    private static string BuildPriceDetails(
        PlaceDetails place)
    {
        var details = new List<string>();

        if (!string.IsNullOrWhiteSpace(place.PriceLevel))
        {
            details.Add(
                $"priceLevel:{Clean(place.PriceLevel)}");
        }

        if (place.RatePerNight is not null)
        {
            details.Add(
                $"nightlyRate:{place.RatePerNight:0.00} " +
                $"{place.PriceCurrency}");
        }

        if (place.TotalRate is not null)
        {
            details.Add(
                $"totalStayRate:{place.TotalRate:0.00} " +
                $"{place.PriceCurrency}");
        }

        if (!string.IsNullOrWhiteSpace(place.PriceSource))
        {
            details.Add(
                $"rateSource:{Clean(place.PriceSource)}");
        }

        return details.Count == 0
            ? string.Empty
            : $"|{string.Join("|", details)}";
    }

    private static string FormatPurpose(
        ItineraryPlacePurpose purpose)
    {
        return purpose switch
        {
            ItineraryPlacePurpose.Meal => "meal",
            ItineraryPlacePurpose.Accommodation =>
                "accommodation",
            _ => "activity"
        };
    }

    private static bool IsUsablePlace(
        PlaceDetails place)
    {
        return
            !string.IsNullOrWhiteSpace(place.Name) &&
            !string.IsNullOrWhiteSpace(place.PlaceId) &&
            !string.IsNullOrWhiteSpace(place.Address);
    }

    private static bool MatchesPurpose(
        PlaceDetails place,
        ItineraryPlacePurpose purpose)
    {
        var types = place.Types
            .Select(type => type.ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (types.Count == 0)
        {
            return true;
        }

        var isMeal = types.Overlaps(
        [
            "restaurant",
            "cafe",
            "bakery",
            "food",
            "meal_delivery",
            "meal_takeaway"
        ]);

        var isAccommodation = types.Overlaps(
        [
            "lodging",
            "hotel",
            "resort_hotel",
            "hostel",
            "motel",
            "bed_and_breakfast",
            "guest_house"
        ]);

        return purpose switch
        {
            ItineraryPlacePurpose.Meal => isMeal,
            ItineraryPlacePurpose.Accommodation =>
                isAccommodation,
            _ => !isMeal && !isAccommodation
        };
    }

    private static async Task<IReadOnlySet<string>>
        ResolveDestinationAliasesAsync(
            IGooglePlacesService googlePlacesService,
            string destination,
            CancellationToken cancellationToken)
    {
        var aliases = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        AddDestinationAlias(
            aliases,
            GetPrimaryDestination(destination));

        var results =
            await googlePlacesService.TextSearchAsync(
                destination,
                cancellationToken,
                "en");

        var resolvedDestination = results.FirstOrDefault(place =>
            place.Types.Any(type =>
                type.Contains(
                    "locality",
                    StringComparison.OrdinalIgnoreCase) ||
                type.Contains(
                    "administrative_area",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    type,
                    "country",
                    StringComparison.OrdinalIgnoreCase)))
            ?? results.FirstOrDefault();

        if (resolvedDestination is not null)
        {
            AddDestinationAlias(
                aliases,
                resolvedDestination.Name);

            AddDestinationAlias(
                aliases,
                GetPrimaryDestination(
                    resolvedDestination.Address));
        }

        return aliases;
    }

    private static void AddDestinationAlias(
        ISet<string> aliases,
        string? value)
    {
        var normalized = NormalizeForComparison(value);

        if (!string.IsNullOrWhiteSpace(normalized))
        {
            aliases.Add(normalized);
        }
    }

    private static bool IsInDestination(
        PlaceDetails place,
        IReadOnlySet<string> destinationAliases)
    {
        var placeText = NormalizeForComparison(
            $"{place.Name} {place.Address}");

        return destinationAliases.Any(alias =>
            placeText.Contains(
                alias,
                StringComparison.OrdinalIgnoreCase) ||
            AliasTokensMatch(alias, placeText));
    }

    private static bool AliasTokensMatch(
        string alias,
        string placeText)
    {
        var ignoredTokens = new HashSet<string>(
            [
                "city",
                "province",
                "governorate",
                "region",
                "state"
            ],
            StringComparer.OrdinalIgnoreCase);

        var tokens = alias
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries)
            .Where(token =>
                token.Length >= 3 &&
                !ignoredTokens.Contains(token))
            .ToList();

        return tokens.Count > 0 &&
               tokens.All(token =>
                   placeText.Contains(
                       token,
                       StringComparison.OrdinalIgnoreCase));
    }

    private static string GetPrimaryDestination(
        string value)
    {
        return value
            .Split(
                [',', ';', '|'],
                StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()
            ?.Trim() ?? value.Trim();
    }

    private static string NormalizeForComparison(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value
            .Normalize(NormalizationForm.FormD);

        var characters = decomposed
            .Where(character =>
                CharUnicodeInfo.GetUnicodeCategory(character) !=
                    UnicodeCategory.NonSpacingMark)
            .Select(character =>
                char.IsLetterOrDigit(character)
                    ? char.ToLowerInvariant(character)
                    : ' ')
            .ToArray();

        return string.Join(
            " ",
            new string(characters)
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries));
    }

    private static string Clean(
        string? value,
        int maximumLength = 200)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Not available";
        }

        var cleaned = value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        return cleaned.Length <= maximumLength
            ? cleaned
            : cleaned[..maximumLength].TrimEnd();
    }

    public static void ResolvePlaceReferences(
    AiItineraryDay day,
    IReadOnlyList<ItineraryPlaceCandidate> places,
    string assignedCity)
    {
        var validReferences = places
            .Select(candidate => candidate.Reference)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var item in day.Items)
        {
            var category =
                ItineraryItemInput.ParseCategory(item.Category);

            if (category == ItineraryItemCategory.Other)
            {
                var matchingOtherPlace = places.FirstOrDefault(candidate =>
                    string.Equals(
                        candidate.City,
                        assignedCity,
                        StringComparison.OrdinalIgnoreCase) &&
                    candidate.Purpose is
                        ItineraryPlacePurpose.Activity or
                        ItineraryPlacePurpose.Meal &&
                    ((!string.IsNullOrWhiteSpace(item.PlaceReference) &&
                      string.Equals(
                          candidate.Reference,
                          item.PlaceReference.Trim(),
                          StringComparison.OrdinalIgnoreCase)) ||
                     NamesMatch(
                         item.Location,
                         candidate.Place.Name) ||
                     NamesMatch(
                         item.Title,
                         candidate.Place.Name)));

                if (matchingOtherPlace is not null)
                {
                    item.PlaceReference =
                        matchingOtherPlace.Reference;
                    item.Location =
                        matchingOtherPlace.Place.Name;
                    item.Category =
                        matchingOtherPlace.Purpose ==
                        ItineraryPlacePurpose.Meal
                            ? "meal"
                            : "activity";
                    category =
                        ItineraryItemInput.ParseCategory(
                            item.Category);
                }
            }

            var requiresVerifiedPlace =
                category is ItineraryItemCategory.Activity
                    or ItineraryItemCategory.Accommodation;

            if (!requiresVerifiedPlace)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(item.PlaceReference) &&
                validReferences.Contains(item.PlaceReference.Trim()))
            {
                continue;
            }

            var matchingPlace = places.FirstOrDefault(candidate =>
                string.Equals(
                    candidate.City,
                    assignedCity,
                    StringComparison.OrdinalIgnoreCase) &&
                (NamesMatch(
                     item.Location,
                     candidate.Place.Name) ||
                 NamesMatch(
                     item.Title,
                     candidate.Place.Name)));

            if (matchingPlace is null)
            {
                continue;
            }

            item.PlaceReference = matchingPlace.Reference;
            item.Location = matchingPlace.Place.Name;
        }
    }

    private static bool NamesMatch(
        string? generatedValue,
        string? officialPlaceName)
    {
        var generated = NormalizeName(generatedValue);
        var official = NormalizeName(officialPlaceName);

        if (string.IsNullOrWhiteSpace(generated) ||
            string.IsNullOrWhiteSpace(official))
        {
            return false;
        }

        return string.Equals(
                   generated,
                   official,
                   StringComparison.OrdinalIgnoreCase) ||
               generated.Contains(
                   official,
                   StringComparison.OrdinalIgnoreCase) ||
               official.Contains(
                   generated,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var characters = value
            .Where(character =>
                char.IsLetterOrDigit(character) ||
                char.IsWhiteSpace(character))
            .ToArray();

        return string.Join(
            " ",
            new string(characters)
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries))
            .Trim();
    }
}
