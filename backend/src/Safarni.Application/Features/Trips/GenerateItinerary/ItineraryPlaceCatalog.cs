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
    string City,
    int? StayNights);

internal enum ItineraryPlacePurpose
{
    Activity,
    Meal,
    Accommodation
}

internal sealed record ItineraryPlaceSearch(
    string Query,
    ItineraryPlacePurpose Purpose);

internal sealed record ItineraryActivityReconciliationResult(
    int RepairedCount,
    int RemovedCount);

internal sealed record ItineraryCatalogSearchResult(
    string City,
    ItineraryPlacePurpose Purpose,
    int? StayNights,
    IReadOnlyList<PlaceDetails> Places);

internal static class ItineraryPlaceCatalog
{
    private const int DiscoveryConcurrency = 3;
    private const int CommerceConcurrency = 2;

    /// <summary>
    /// Builds a reusable, image-rich catalog directly from SerpAPI. Queries are
    /// category based instead of being generated once per AI suggestion, which
    /// keeps search count and latency bounded as trip length increases.
    /// </summary>
    public static async Task<IReadOnlyList<ItineraryPlaceCandidate>>
        LoadSerpPrimaryAsync(
            IItineraryTravelDataService? travelDataService,
            Trip trip,
            IReadOnlyList<ItineraryCityStop> cityPlan,
            GenerateItineraryPreferences preferences,
            string currencyCode,
            int travelerCount,
            IReadOnlySet<string>? previouslyUsedPlaceIds,
            CancellationToken cancellationToken)
    {
        if (travelDataService?.IsConfigured != true)
        {
            return [];
        }

        var previousIds = previouslyUsedPlaceIds ??
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var discoveryGate = new SemaphoreSlim(
            DiscoveryConcurrency,
            DiscoveryConcurrency);
        using var commerceGate = new SemaphoreSlim(
            CommerceConcurrency,
            CommerceConcurrency);
        var searches = new List<Task<ItineraryCatalogSearchResult>>();

        foreach (var cityStop in cityPlan)
        {
            foreach (var search in BuildPrimaryDiscoveryQueries(
                         cityStop.City,
                         preferences,
                         previousIds.Count > 0))
            {
                searches.Add(SearchLocalCatalogAsync(
                    travelDataService,
                    discoveryGate,
                    cityStop.City,
                    search,
                    preferences.PreferredLanguage,
                    cancellationToken));
            }

            if (!string.Equals(
                    preferences.AccommodationStyle,
                    "already_booked",
                    StringComparison.OrdinalIgnoreCase) &&
                cityStop.GetCheckOutDate(trip) is { } checkOutDate)
            {
                searches.Add(SearchHotelCatalogAsync(
                    travelDataService,
                    commerceGate,
                    trip,
                    cityStop,
                    checkOutDate,
                    preferences,
                    currencyCode,
                    travelerCount,
                    cancellationToken));
            }
        }

        var searchResults = await Task.WhenAll(searches);
        var selected = new List<(
            PlaceDetails Place,
            ItineraryPlacePurpose Purpose,
            string City,
            int? StayNights)>();

        foreach (var cityStop in cityPlan)
        {
            foreach (var purpose in Enum.GetValues<ItineraryPlacePurpose>())
            {
                var candidates = searchResults
                    .Where(result =>
                        result.Purpose == purpose &&
                        string.Equals(
                            result.City,
                            cityStop.City,
                            StringComparison.OrdinalIgnoreCase))
                    .SelectMany(result => result.Places.Select(place =>
                        (Place: place, result.StayNights)))
                    .Where(candidate => IsUsablePlace(candidate.Place))
                    // Every SerpAPI query is explicitly scoped with
                    // "in {city}". Do not reject valid localized addresses
                    // merely because they use another script or spelling.
                    .Where(candidate =>
                        MatchesPurpose(candidate.Place, purpose))
                    .GroupBy(
                        candidate => candidate.Place.PlaceId,
                        StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .ToList();

                var requiredCount = purpose switch
                {
                    ItineraryPlacePurpose.Activity =>
                        cityStop.DayCount *
                        GetActivitiesPerDay(preferences.Pace),
                    ItineraryPlacePurpose.Meal =>
                        cityStop.DayCount * 3,
                    ItineraryPlacePurpose.Accommodation => 1,
                    _ => 1
                };
                var poolLimit = purpose switch
                {
                    ItineraryPlacePurpose.Activity =>
                        Math.Min(30, Math.Max(12, requiredCount * 2)),
                    ItineraryPlacePurpose.Meal =>
                        Math.Min(20, Math.Max(8, requiredCount)),
                    ItineraryPlacePurpose.Accommodation => 3,
                    _ => 10
                };

                var unused = OrderCatalogCandidates(
                        candidates.Where(candidate =>
                            !previousIds.Contains(
                                candidate.Place.PlaceId)),
                        purpose,
                        preferences.AccommodationStyle)
                    .ToList();
                var pool = unused.Count >= requiredCount
                    ? unused.Take(poolLimit)
                    : unused.Concat(OrderCatalogCandidates(
                            candidates.Where(candidate =>
                                previousIds.Contains(
                                    candidate.Place.PlaceId)),
                            purpose,
                            preferences.AccommodationStyle))
                        .Take(poolLimit);

                selected.AddRange(pool.Select(candidate =>
                    (
                        candidate.Place,
                        purpose,
                        cityStop.City,
                        candidate.StayNights)));
            }
        }

        return selected
            .GroupBy(
                candidate =>
                    $"{candidate.City}:{candidate.Purpose}:" +
                    candidate.Place.PlaceId,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select((candidate, index) =>
                new ItineraryPlaceCandidate(
                    $"P{index + 1}",
                    candidate.Place,
                    candidate.Purpose,
                    candidate.City,
                    candidate.StayNights))
            .ToList();
    }

    private static IReadOnlyList<ItineraryPlaceSearch>
        BuildPrimaryDiscoveryQueries(
            string destination,
            GenerateItineraryPreferences preferences,
            bool isRegeneration)
    {
        var interestPhrases = preferences.Interests
            .Where(interest => !IsFoodInterest(interest))
            .Select(BuildInterestSearchPhrase)
            .Where(phrase => !string.IsNullOrWhiteSpace(phrase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
        var interestText = interestPhrases.Count == 0
            ? "top rated attractions and local experiences"
            : "top rated attractions and " +
              string.Join(" and ", interestPhrases);
        var alternativeText = isRegeneration
            ? "hidden gems unique attractions and local experiences"
            : "must see landmarks and unique experiences";

        return
        [
            new ItineraryPlaceSearch(
                $"{interestText} in {destination}",
                ItineraryPlacePurpose.Activity),
            new ItineraryPlaceSearch(
                $"{alternativeText} in {destination}",
                ItineraryPlacePurpose.Activity),
            new ItineraryPlaceSearch(
                $"best rated local restaurants and cafes in {destination}",
                ItineraryPlacePurpose.Meal)
        ];
    }

    private static async Task<ItineraryCatalogSearchResult>
        SearchLocalCatalogAsync(
            IItineraryTravelDataService travelDataService,
            SemaphoreSlim gate,
            string city,
            ItineraryPlaceSearch search,
            string languageCode,
            CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var places = await travelDataService.SearchLocalAsync(
                search.Query,
                city,
                languageCode,
                cancellationToken);
            return new ItineraryCatalogSearchResult(
                city,
                search.Purpose,
                null,
                places);
        }
        finally
        {
            gate.Release();
        }
    }

    private static async Task<ItineraryCatalogSearchResult>
        SearchHotelCatalogAsync(
            IItineraryTravelDataService travelDataService,
            SemaphoreSlim gate,
            Trip trip,
            ItineraryCityStop cityStop,
            DateTime checkOutDate,
            GenerateItineraryPreferences preferences,
            string currencyCode,
            int travelerCount,
            CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var places = await travelDataService.SearchHotelsAsync(
                cityStop.City,
                preferences.AccommodationStyle,
                cityStop.GetStartDate(trip),
                checkOutDate,
                travelerCount,
                currencyCode,
                preferences.PreferredLanguage,
                cancellationToken);
            return new ItineraryCatalogSearchResult(
                cityStop.City,
                ItineraryPlacePurpose.Accommodation,
                cityStop.NightCount,
                places);
        }
        finally
        {
            gate.Release();
        }
    }

    private static IEnumerable<(
        PlaceDetails Place,
        int? StayNights)> OrderCatalogCandidates(
            IEnumerable<(PlaceDetails Place, int? StayNights)> candidates,
            ItineraryPlacePurpose purpose,
            string accommodationStyle)
    {
        if (purpose == ItineraryPlacePurpose.Accommodation)
        {
            return OrderAccommodationCandidates(
                    candidates.Select(candidate =>
                        new ItineraryPlaceCandidate(
                            string.Empty,
                            candidate.Place,
                            purpose,
                            string.Empty,
                            candidate.StayNights)),
                    accommodationStyle)
                .Select(candidate =>
                    (candidate.Place, candidate.StayNights));
        }

        return candidates
            .OrderByDescending(candidate =>
                !string.IsNullOrWhiteSpace(
                    candidate.Place.PhotoUrl))
            .ThenByDescending(candidate =>
                candidate.Place.Rating ?? 0)
            .ThenByDescending(candidate =>
                candidate.Place.UserRatingsTotal ?? 0);
    }

    public static async Task<IReadOnlyList<ItineraryVerifiedActivity>>
        VerifyActivitySuggestionsAsync(
            IGooglePlacesService googlePlacesService,
            IItineraryTravelDataService? travelDataService,
            IReadOnlyList<ItineraryActivitySuggestion> suggestions,
            IReadOnlyList<ItineraryCityStop> cityPlan,
            string languageCode,
            CancellationToken cancellationToken)
    {
        var verified = new List<ItineraryVerifiedActivity>();
        var usedPlaceIds = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var cityStop in cityPlan)
        {
            var cityAliases =
                await ResolveDestinationAliasesAsync(
                    googlePlacesService,
                    cityStop.City,
                    cancellationToken);

            var citySuggestions = suggestions
                .Where(suggestion =>
                    string.Equals(
                        suggestion.City,
                        cityStop.City,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var suggestion in citySuggestions)
            {
                var results = await SearchSuggestedActivityAsync(
                    googlePlacesService,
                    travelDataService,
                    suggestion,
                    cityStop.City,
                    cityAliases,
                    languageCode,
                    cancellationToken);

                var selected = results
                    .Where(IsUsablePlace)
                    .Where(place =>
                        IsInDestination(
                            place,
                            cityAliases))
                    .Where(place =>
                        MatchesPurpose(
                            place,
                            ItineraryPlacePurpose.Activity))
                    .Select(place => new
                    {
                        Place = place,
                        Score = CalculateSuggestionMatchScore(
                            suggestion.Name,
                            place)
                    })
                    .Where(result => result.Score >= 55d)
                    .OrderByDescending(result => result.Score)
                    .ThenByDescending(result =>
                        !string.IsNullOrWhiteSpace(
                            result.Place.PhotoUrl))
                    .ThenByDescending(result =>
                        result.Place.Rating ?? 0)
                    .ThenByDescending(result =>
                        result.Place.UserRatingsTotal ?? 0)
                    .FirstOrDefault();

                if (selected is null ||
                    !usedPlaceIds.Add(selected.Place.PlaceId))
                {
                    continue;
                }

                verified.Add(
                    new ItineraryVerifiedActivity(
                        selected.Place,
                        cityStop.City,
                        suggestion.Name,
                        selected.Score));
            }
        }

        return verified;
    }

    public static IReadOnlyList<ItineraryPlaceCandidate>
        MergeSuggestedActivities(
            IReadOnlyList<ItineraryPlaceCandidate> fallbackPlaces,
            IReadOnlyList<ItineraryVerifiedActivity> verifiedActivities,
            IReadOnlyList<ItineraryCityStop> cityPlan,
            GenerateItineraryPreferences preferences)
    {
        var selected = new List<(
            PlaceDetails Place,
            ItineraryPlacePurpose Purpose,
            string City,
            int? StayNights)>();

        foreach (var cityStop in cityPlan)
        {
            var activityLimit = GetCandidateLimit(
                ItineraryPlacePurpose.Activity,
                cityStop.DayCount,
                preferences.Pace);

            var preferredActivities = verifiedActivities
                .Where(activity =>
                    string.Equals(
                        activity.City,
                        cityStop.City,
                        StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(activity =>
                    !string.IsNullOrWhiteSpace(
                        activity.Place.PhotoUrl))
                .ThenByDescending(activity =>
                    activity.MatchScore)
                .ThenByDescending(activity =>
                    activity.Place.Rating ?? 0)
                .Select(activity => activity.Place)
                .ToList();

            var fallbackActivities = fallbackPlaces
                .Where(candidate =>
                    candidate.Purpose ==
                        ItineraryPlacePurpose.Activity &&
                    string.Equals(
                        candidate.City,
                        cityStop.City,
                        StringComparison.OrdinalIgnoreCase))
                .Select(candidate => candidate.Place)
                .ToList();

            var cityActivities = preferredActivities
                .Concat(fallbackActivities)
                .GroupBy(
                    place => place.PlaceId,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .Take(activityLimit)
                .ToList();

            selected.AddRange(cityActivities.Select(place =>
                (
                    place,
                    ItineraryPlacePurpose.Activity,
                    cityStop.City,
                    (int?)null)));

            selected.AddRange(fallbackPlaces
                .Where(candidate =>
                    candidate.Purpose ==
                        ItineraryPlacePurpose.Meal &&
                    string.Equals(
                        candidate.City,
                        cityStop.City,
                        StringComparison.OrdinalIgnoreCase))
                .Take(cityStop.DayCount * 3)
                .Select(candidate =>
                    (
                        candidate.Place,
                        candidate.Purpose,
                        candidate.City,
                        candidate.StayNights)));

            var accommodations = OrderAccommodationCandidates(
                    fallbackPlaces.Where(candidate =>
                        candidate.Purpose ==
                            ItineraryPlacePurpose.Accommodation &&
                        string.Equals(
                            candidate.City,
                            cityStop.City,
                            StringComparison.OrdinalIgnoreCase)),
                    preferences.AccommodationStyle)
                .Take(3);

            selected.AddRange(accommodations.Select(candidate =>
                (
                    candidate.Place,
                    candidate.Purpose,
                    candidate.City,
                    candidate.StayNights)));
        }

        return selected
            .GroupBy(
                candidate =>
                    $"{candidate.City}:{candidate.Purpose}:" +
                    candidate.Place.PlaceId,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select((candidate, index) =>
                new ItineraryPlaceCandidate(
                    $"P{index + 1}",
                    candidate.Place,
                    candidate.Purpose,
                    candidate.City,
                    candidate.StayNights))
            .ToList();
    }

    public static int NormalizeActivityItems(
        IReadOnlyList<AiItineraryDay> days,
        IReadOnlyList<ItineraryPlaceCandidate> places,
        string assignedCity,
        string languageCode)
    {
        return ReconcileActivityItems(
            days,
            places,
            assignedCity,
            languageCode).RepairedCount;
    }

    public static ItineraryActivityReconciliationResult
        ReconcileActivityItems(
            IReadOnlyList<AiItineraryDay> days,
            IReadOnlyList<ItineraryPlaceCandidate> places,
            string assignedCity,
            string languageCode)
    {
        var activityCandidates = places
            .Where(candidate =>
                candidate.Purpose ==
                    ItineraryPlacePurpose.Activity &&
                string.Equals(
                    candidate.City,
                    assignedCity,
                    StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(candidate =>
                !string.IsNullOrWhiteSpace(
                    candidate.Place.PhotoUrl))
            .ThenByDescending(candidate =>
                candidate.Place.Rating ?? 0)
            .ThenByDescending(candidate =>
                candidate.Place.UserRatingsTotal ?? 0)
            .ToList();

        var activityByReference = activityCandidates
            .ToDictionary(
                candidate => candidate.Reference,
                StringComparer.OrdinalIgnoreCase);

        var reservedReferences = days
            .SelectMany(day => day.Items)
            .Where(item =>
                ItineraryItemInput.ParseCategory(item.Category) ==
                    ItineraryItemCategory.Activity &&
                !string.IsNullOrWhiteSpace(item.PlaceReference) &&
                activityByReference.ContainsKey(
                    item.PlaceReference.Trim()))
            .Select(item => item.PlaceReference!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var usedReferences = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        var repairedCount = 0;
        var removedCount = 0;

        foreach (var day in days.OrderBy(day => day.DayNumber))
        {
            var itemsToRemove = new List<AiItineraryItem>();

            foreach (var item in day.Items
                         .OrderBy(entry => entry.OrderIndex))
            {
                var category =
                    ItineraryItemInput.ParseCategory(item.Category);

                var isActivityLikeOther =
                    category == ItineraryItemCategory.Other &&
                    !IsRestOrAdministrativeItem(item);

                var isActivity =
                    category == ItineraryItemCategory.Activity;

                if (!isActivity && !isActivityLikeOther)
                {
                    continue;
                }

                if (isActivity &&
                    !string.IsNullOrWhiteSpace(item.PlaceReference) &&
                    activityByReference.TryGetValue(
                        item.PlaceReference.Trim(),
                        out var verifiedCandidate))
                {
                    CanonicalizeVerifiedActivity(
                        item,
                        verifiedCandidate,
                        languageCode,
                        replaceVisibleText: false);
                    usedReferences.Add(
                        verifiedCandidate.Reference);
                    continue;
                }

                var replacement = FindActivityReplacement(
                    item,
                    activityCandidates,
                    usedReferences,
                    reservedReferences);

                if (replacement is null)
                {
                    // Never persist an unverified venue. When every external
                    // provider failed to verify a candidate, keep the rest of
                    // the day and remove only the unsupported activity item.
                    itemsToRemove.Add(item);
                    removedCount++;
                    continue;
                }

                CanonicalizeVerifiedActivity(
                    item,
                    replacement,
                    languageCode,
                    replaceVisibleText: true);
                usedReferences.Add(replacement.Reference);
                repairedCount++;
            }

            foreach (var item in itemsToRemove)
            {
                day.Items.Remove(item);
            }

            ReindexDay(day);
        }

        return new ItineraryActivityReconciliationResult(
            repairedCount,
            removedCount);
    }

    private static ItineraryPlaceCandidate?
        FindActivityReplacement(
            AiItineraryItem item,
            IReadOnlyList<ItineraryPlaceCandidate> candidates,
            IReadOnlySet<string> usedReferences,
            IReadOnlySet<string> reservedReferences)
    {
        var unusedCandidates = candidates
            .Where(candidate =>
                !usedReferences.Contains(candidate.Reference) &&
                !reservedReferences.Contains(candidate.Reference))
            .ToList();

        var matchingCandidate = unusedCandidates
            .Select(candidate => new
            {
                Candidate = candidate,
                Score = Math.Max(
                    CalculateNameSimilarity(
                        item.Location,
                        candidate.Place.Name),
                    CalculateNameSimilarity(
                        item.Title,
                        candidate.Place.Name))
            })
            .Where(entry => entry.Score >= 45d)
            .OrderByDescending(entry => entry.Score)
            .ThenByDescending(entry =>
                !string.IsNullOrWhiteSpace(
                    entry.Candidate.Place.PhotoUrl))
            .ThenByDescending(entry =>
                entry.Candidate.Place.Rating ?? 0)
            .Select(entry => entry.Candidate)
            .FirstOrDefault();

        if (matchingCandidate is not null)
        {
            return matchingCandidate;
        }

        // Never duplicate a verified attraction just to satisfy an AI-created
        // count. When the verified catalog is exhausted, the caller removes
        // only the unsupported extra item and keeps the rest of the day.
        return unusedCandidates
            .OrderByDescending(candidate =>
                !string.IsNullOrWhiteSpace(
                    candidate.Place.PhotoUrl))
            .ThenByDescending(candidate =>
                candidate.Place.Rating ?? 0)
            .ThenByDescending(candidate =>
                candidate.Place.UserRatingsTotal ?? 0)
            .FirstOrDefault();
    }

    private static void CanonicalizeVerifiedActivity(
        AiItineraryItem item,
        ItineraryPlaceCandidate candidate,
        string languageCode,
        bool replaceVisibleText)
    {
        item.Category = "activity";
        item.PlaceReference = candidate.Reference;
        item.Location = candidate.Place.Name;

        var titleUsesWrongScript =
            ItineraryTextPolicy.IsArabicLanguage(languageCode)
                ? !ItineraryTextPolicy.ContainsArabic(item.Title)
                : ItineraryTextPolicy.ContainsArabic(item.Title);

        var descriptionUsesWrongScript =
            ItineraryTextPolicy.IsArabicLanguage(languageCode)
                ? !ItineraryTextPolicy.ContainsArabic(item.Description)
                : ItineraryTextPolicy.ContainsArabic(item.Description);

        if (replaceVisibleText || titleUsesWrongScript)
        {
            item.Title = ItineraryTextPolicy.BuildActivityTitle(
                candidate.Place,
                languageCode);
        }

        if (replaceVisibleText || descriptionUsesWrongScript)
        {
            item.Description =
                ItineraryTextPolicy.BuildActivityDescription(
                    languageCode);
        }
    }

    private static void ReindexDay(AiItineraryDay day)
    {
        for (var index = 0;
             index < day.Items.Count;
             index++)
        {
            day.Items[index].OrderIndex = index;
        }
    }

    public static void ValidateRemainingOtherItems(
        IReadOnlyList<AiItineraryDay> days)
    {
        foreach (var day in days)
        {
            foreach (var item in day.Items)
            {
                if (ItineraryItemInput.ParseCategory(
                        item.Category) !=
                    ItineraryItemCategory.Other)
                {
                    continue;
                }

                if (!IsRestOrAdministrativeItem(item))
                {
                    throw new ItineraryOutputException(
                        $"The item '{item.Title}' looks like an activity but was returned as other. It must use a verified activity placeReference.");
                }

                item.PlaceReference = null;
            }
        }
    }

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
                        .OrderByDescending(place =>
                            search.Purpose ==
                                ItineraryPlacePurpose.Activity &&
                            !string.IsNullOrWhiteSpace(
                                place.PhotoUrl))
                        .ThenByDescending(
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
                            // Keep a broad provider-backed hotel pool here.
                            // MergeSuggestedActivities applies the requested
                            // budget/mid-range/luxury ordering afterward.
                            .Take(12)))
            .Select((candidate, index) =>
            {
                var cityStop = cityPlan.First(stop =>
                    string.Equals(
                        stop.City,
                        candidate.City,
                        StringComparison.OrdinalIgnoreCase));

                return new ItineraryPlaceCandidate(
                    $"P{index + 1}",
                    candidate.Place,
                    candidate.Purpose,
                    candidate.City,
                    candidate.Purpose == ItineraryPlacePurpose.Accommodation
                        ? cityStop.NightCount
                        : null);
            })
            .ToList();

        return selectedPlaces;
    }

    private static async Task<IReadOnlyList<PlaceDetails>>
        SearchSuggestedActivityAsync(
            IGooglePlacesService googlePlacesService,
            IItineraryTravelDataService? travelDataService,
            ItineraryActivitySuggestion suggestion,
            string destination,
            IReadOnlySet<string> destinationAliases,
            string languageCode,
            CancellationToken cancellationToken)
    {
        var query = $"{suggestion.Name} in {destination}";
        IReadOnlyList<PlaceDetails> results = [];

        if (travelDataService?.IsConfigured == true)
        {
            results = await travelDataService.SearchLocalAsync(
                query,
                destination,
                languageCode,
                cancellationToken);
        }

        var hasStrongPhotoMatch = results.Any(place =>
            IsUsablePlace(place) &&
            IsInDestination(place, destinationAliases) &&
            MatchesPurpose(
                place,
                ItineraryPlacePurpose.Activity) &&
            CalculateSuggestionMatchScore(
                suggestion.Name,
                place) >= 55d &&
            !string.IsNullOrWhiteSpace(place.PhotoUrl));

        if (hasStrongPhotoMatch)
        {
            return results;
        }

        var googleResults =
            await googlePlacesService.TextSearchAsync(
                query,
                cancellationToken,
                languageCode);

        return results
            .Concat(googleResults)
            .GroupBy(
                place => string.IsNullOrWhiteSpace(place.PlaceId)
                    ? NormalizeName(
                        $"{place.Name} {place.Address}")
                    : place.PlaceId,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(place =>
                    !string.IsNullOrWhiteSpace(place.PhotoUrl))
                .ThenByDescending(place => place.Rating ?? 0)
                .ThenByDescending(place =>
                    place.UserRatingsTotal ?? 0)
                .First())
            .ToList();
    }

    private static double CalculateSuggestionMatchScore(
        string suggestionName,
        PlaceDetails place)
    {
        var score = CalculateNameSimilarity(
            suggestionName,
            place.Name);

        if (!string.IsNullOrWhiteSpace(place.PhotoUrl))
        {
            score += 4d;
        }

        if (place.Rating is >= 4d)
        {
            score += 2d;
        }

        return score;
    }

    private static double CalculateNameSimilarity(
        string? first,
        string? second)
    {
        var normalizedFirst = NormalizeName(first);
        var normalizedSecond = NormalizeName(second);

        if (string.IsNullOrWhiteSpace(normalizedFirst) ||
            string.IsNullOrWhiteSpace(normalizedSecond))
        {
            return 0d;
        }

        if (string.Equals(
                normalizedFirst,
                normalizedSecond,
                StringComparison.OrdinalIgnoreCase))
        {
            return 100d;
        }

        if (normalizedFirst.Contains(
                normalizedSecond,
                StringComparison.OrdinalIgnoreCase) ||
            normalizedSecond.Contains(
                normalizedFirst,
                StringComparison.OrdinalIgnoreCase))
        {
            return 88d;
        }

        var firstTokens = normalizedFirst
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length >= 2)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var secondTokens = normalizedSecond
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length >= 2)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (firstTokens.Count == 0 ||
            secondTokens.Count == 0)
        {
            return 0d;
        }

        var sharedCount = firstTokens
            .Count(secondTokens.Contains);
        var recall = sharedCount /
                     (double)firstTokens.Count;
        var precision = sharedCount /
                        (double)secondTokens.Count;

        return (recall * 65d) +
               (precision * 35d);
    }

    private static IOrderedEnumerable<ItineraryPlaceCandidate>
        OrderAccommodationCandidates(
            IEnumerable<ItineraryPlaceCandidate> candidates,
            string accommodationStyle)
    {
        var style = accommodationStyle
            .Trim()
            .ToLowerInvariant();

        return style switch
        {
            "budget" => candidates
                .OrderBy(candidate =>
                    candidate.Place.TotalRate is null
                        ? 1
                        : 0)
                .ThenBy(candidate =>
                    candidate.Place.TotalRate ??
                    decimal.MaxValue)
                .ThenByDescending(candidate =>
                    candidate.Place.Rating ?? 0),

            "luxury" => candidates
                .OrderByDescending(candidate =>
                    candidate.Place.Rating ?? 0)
                .ThenByDescending(candidate =>
                    candidate.Place.UserRatingsTotal ?? 0)
                .ThenByDescending(candidate =>
                    candidate.Place.TotalRate ?? 0),

            _ => candidates
                .OrderByDescending(candidate =>
                    candidate.Place.Rating ?? 0)
                .ThenByDescending(candidate =>
                    candidate.Place.UserRatingsTotal ?? 0)
                .ThenBy(candidate =>
                    candidate.Place.TotalRate ??
                    decimal.MaxValue)
        };
    }

    private static bool IsRestOrAdministrativeItem(
        AiItineraryItem item)
    {
        var text = NormalizeName(
            $"{item.Title} {item.Description}");

        var administrativePhrases = new[]
        {
            "rest",
            "free time",
            "break",
            "check in",
            "check out",
            "checkin",
            "checkout",
            "hotel check in",
            "hotel check out",
            "hotel checkin",
            "hotel checkout",
            "arrival",
            "departure",
            "settle in",
            "personal time",
            "airport preparation",
            "prepare for airport",
            "prepare for departure",
            "pack bags",
            "stroll",
            "relax",
            "unwind",
            "leisure time",
            "quiet evening",
            "enjoy the resort",
            "resort time",
            "نزهة",
            "تمشية",
            "استرخاء",
            "الاسترخاء",
            "وقت ترفيهي",
            "أمسية هادئة",
            "راحة",
            "وقت حر",
            "استراحة",
            "تسجيل الوصول",
            "تسجيل المغادرة",
            "الوصول",
            "المغادرة",
            "الاستعداد للمطار",
            "الاستعداد للمغادرة",
            "حزم الأمتعة",
            "الاستقرار في الفندق"
        };

        return administrativePhrases.Any(phrase =>
            text.Contains(
                phrase,
                StringComparison.OrdinalIgnoreCase));
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
                var checkOutDate = cityStop.GetCheckOutDate(trip);

                if (checkOutDate is null)
                {
                    return [];
                }

                results =
                    await travelDataService.SearchHotelsAsync(
                        cityStop.City,
                        preferences.AccommodationStyle,
                        cityStop.GetStartDate(trip),
                        checkOutDate.Value,
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

        if (results.Count > 0 &&
            (search.Purpose == ItineraryPlacePurpose.Accommodation ||
             results.Any(place =>
                 !string.IsNullOrWhiteSpace(place.PhotoUrl))))
        {
            return results;
        }

        var googleResults =
            await googlePlacesService.TextSearchAsync(
                search.Query,
                cancellationToken,
                preferences.PreferredLanguage);

        return results
            .Concat(googleResults)
            .GroupBy(
                place => string.IsNullOrWhiteSpace(place.PlaceId)
                    ? NormalizeName(
                        $"{place.Name} {place.Address}")
                    : place.PlaceId,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(place =>
                    !string.IsNullOrWhiteSpace(place.PhotoUrl))
                .ThenByDescending(place => place.Rating ?? 0)
                .ThenByDescending(place =>
                    place.UserRatingsTotal ?? 0)
                .First())
            .ToList();
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

        foreach (var cityGroup in places
                     .GroupBy(
                         candidate => candidate.City,
                         StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine(
                $"CITY CATALOG: {Clean(cityGroup.Key)}");

            foreach (var candidate in cityGroup)
            {
                var place = candidate.Place;

                var rating = place.Rating?.ToString(
                    "0.0",
                    CultureInfo.InvariantCulture) ?? "N/A";

                var types = place.Types.Count > 0
                    ? place.Types[0]
                    : "N/A";

                var priceDetails = BuildPriceDetails(place);
                var geographyDetails = BuildGeographyDetails(place);

                builder.AppendLine(
                    $"{candidate.Reference}|" +
                    $"purpose:{FormatPurpose(candidate.Purpose)}|" +
                    $"city:{Clean(candidate.City)}|" +
                    $"name:{Clean(place.Name)}|" +
                    $"address:{Clean(place.Address, 100)}|" +
                    $"rating:{rating}|" +
                    $"types:{Clean(types)}" +
                    geographyDetails +
                    priceDetails);
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string BuildGeographyDetails(
        PlaceDetails place)
    {
        if (place.Latitude is null || place.Longitude is null)
        {
            return string.Empty;
        }

        var latitude = place.Latitude.Value.ToString(
            "0.00000",
            CultureInfo.InvariantCulture);
        var longitude = place.Longitude.Value.ToString(
            "0.00000",
            CultureInfo.InvariantCulture);
        var areaLatitude = Math.Round(
            place.Latitude.Value,
            2,
            MidpointRounding.AwayFromZero).ToString(
                "0.00",
                CultureInfo.InvariantCulture);
        var areaLongitude = Math.Round(
            place.Longitude.Value,
            2,
            MidpointRounding.AwayFromZero).ToString(
                "0.00",
                CultureInfo.InvariantCulture);

        return $"|coordinates:{latitude},{longitude}" +
               $"|areaCluster:{areaLatitude},{areaLongitude}";
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
        var candidatesByReference = places
            .ToDictionary(
                candidate => candidate.Reference,
                StringComparer.OrdinalIgnoreCase);

        foreach (var item in day.Items)
        {
            var category =
                ItineraryItemInput.ParseCategory(item.Category);

            if (category == ItineraryItemCategory.Other)
            {
                var matchingActivity = places.FirstOrDefault(candidate =>
                    candidate.Purpose ==
                        ItineraryPlacePurpose.Activity &&
                    string.Equals(
                        candidate.City,
                        assignedCity,
                        StringComparison.OrdinalIgnoreCase) &&
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

                if (matchingActivity is not null)
                {
                    item.PlaceReference = matchingActivity.Reference;
                    item.Location = matchingActivity.Place.Name;
                    item.Category = "activity";
                    category = ItineraryItemCategory.Activity;
                }
            }

            var requiredPurpose = category switch
            {
                ItineraryItemCategory.Activity =>
                    ItineraryPlacePurpose.Activity,
                ItineraryItemCategory.Meal =>
                    ItineraryPlacePurpose.Meal,
                ItineraryItemCategory.Accommodation =>
                    ItineraryPlacePurpose.Accommodation,
                _ => (ItineraryPlacePurpose?)null
            };

            if (requiredPurpose is null)
            {
                continue;
            }

            ItineraryPlaceCandidate? referencedCandidate = null;

            if (!string.IsNullOrWhiteSpace(item.PlaceReference))
            {
                candidatesByReference.TryGetValue(
                    item.PlaceReference.Trim(),
                    out referencedCandidate);
            }

            if (referencedCandidate is not null &&
                referencedCandidate.Purpose == requiredPurpose.Value &&
                string.Equals(
                    referencedCandidate.City,
                    assignedCity,
                    StringComparison.OrdinalIgnoreCase))
            {
                item.PlaceReference = referencedCandidate.Reference;
                item.Location = referencedCandidate.Place.Name;
                continue;
            }

            var matchingPlace = places.FirstOrDefault(candidate =>
                candidate.Purpose == requiredPurpose.Value &&
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

            // A full-trip request exposes candidates for every destination.
            // If Gemini attaches a meal reference from another city, repair it
            // locally instead of spending another AI request on the same plan.
            if (matchingPlace is null &&
                requiredPurpose == ItineraryPlacePurpose.Meal)
            {
                matchingPlace = places
                    .Where(candidate =>
                        candidate.Purpose ==
                            ItineraryPlacePurpose.Meal &&
                        string.Equals(
                            candidate.City,
                            assignedCity,
                            StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(candidate =>
                        candidate.Place.Rating ?? 0)
                    .ThenByDescending(candidate =>
                        candidate.Place.UserRatingsTotal ?? 0)
                    .FirstOrDefault();
            }

            if (matchingPlace is null)
            {
                continue;
            }

            var titleNamesWrongReferencedPlace =
                referencedCandidate is not null &&
                NamesMatch(
                    item.Title,
                    referencedCandidate.Place.Name);

            item.PlaceReference = matchingPlace.Reference;
            item.Location = matchingPlace.Place.Name;

            if (requiredPurpose == ItineraryPlacePurpose.Meal &&
                titleNamesWrongReferencedPlace)
            {
                item.Title = matchingPlace.Place.Name;
            }
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
