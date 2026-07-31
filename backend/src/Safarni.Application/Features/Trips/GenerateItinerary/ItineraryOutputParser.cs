using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

internal sealed class ItineraryOutputException : Exception
{
    public ItineraryOutputException(string message) : base(message)
    {
    }
}

internal sealed class AiItineraryDay
{
    public int DayNumber { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<AiItineraryItem> Items { get; set; } = new();
}

internal sealed class AiItineraryItem
{
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }

    public string? PlaceReference { get; set; }

    public string Category { get; set; } = "activity";
    public decimal? EstimatedCost { get; set; }
    public int OrderIndex { get; set; }
}
internal static class ItineraryOutputParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyList<AiItineraryDay> ParseAll(string output, Trip trip)
    {
        var days = ExtractJsonObjects(output)
            .Select(ParseDay)
            .ToList();

        ValidateComplete(days, trip);
        return days;
    }

    public static IReadOnlyList<AiItineraryDay> ParseRange(
        string output,
        Trip trip,
        int startDayNumber,
        int endDayNumber)
    {
        var days = ExtractJsonObjects(output)
            .Select(ParseDay)
            .ToList();

        ValidateRange(
            days,
            trip,
            startDayNumber,
            endDayNumber);

        return days;
    }

    public static AiItineraryDay ParseDay(string line)
    {
        try
        {
            var day = JsonSerializer.Deserialize<AiItineraryDay>(line, JsonOptions)
                ?? throw new ItineraryOutputException("The day line was empty.");

            ValidateDay(day);
            return day;
        }
        catch (JsonException exception)
        {
            throw new ItineraryOutputException(
                $"The AI returned invalid JSON: {exception.Message}");
        }
    }

    public static void ValidateComplete(
        IReadOnlyCollection<AiItineraryDay> days,
        Trip trip)
    {
        var expectedCount = (trip.EndDate.Date - trip.StartDate.Date).Days + 1;

        ValidateRange(
            days,
            trip,
            1,
            expectedCount);
    }

    private static void ValidateRange(
        IReadOnlyCollection<AiItineraryDay> days,
        Trip trip,
        int startDayNumber,
        int endDayNumber)
    {
        if (startDayNumber <= 0 ||
            endDayNumber < startDayNumber)
        {
            throw new ItineraryOutputException(
                "The requested itinerary day range is invalid.");
        }

        var expectedCount =
            endDayNumber - startDayNumber + 1;

        if (days.Count != expectedCount)
        {
            throw new ItineraryOutputException(
                $"Expected {expectedCount} days but received {days.Count}.");
        }

        var orderedDays = days.OrderBy(day => day.DayNumber).ToList();

        for (var index = 0; index < expectedCount; index++)
        {
            var day = orderedDays[index];
            var expectedNumber =
                startDayNumber + index;
            var expectedDate = trip.StartDate.Date.AddDays(
                expectedNumber - 1);

            if (day.DayNumber != expectedNumber)
            {
                throw new ItineraryOutputException("The day numbers are incomplete or duplicated.");
            }

            if (!DateTime.TryParseExact(
                    day.Date,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var actualDate) || actualDate.Date != expectedDate)
            {
                throw new ItineraryOutputException("The itinerary dates do not match the trip dates.");
            }
        }
    }

    public static void ValidatePlaceReferences(
    IEnumerable<AiItineraryDay> days,
    IReadOnlyList<ItineraryPlaceCandidate> places,
    IReadOnlyList<ItineraryCityStop> cityPlan)
{
    var candidatesByReference = places
        .ToDictionary(
            candidate => candidate.Reference,
            StringComparer.OrdinalIgnoreCase);

    var usedReferences = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase);

    foreach (var day in days)
    {
        var assignedCity =
            ItineraryCityPlanner.GetStopForDay(
                cityPlan,
                day.DayNumber).City;

        foreach (var item in day.Items)
        {
            var category =
                ItineraryItemInput.ParseCategory(item.Category);

            var requiresVerifiedPlace =
                category is ItineraryItemCategory.Activity
                    or ItineraryItemCategory.Accommodation;

            if (!requiresVerifiedPlace)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.PlaceReference) ||
                !candidatesByReference.TryGetValue(
                    item.PlaceReference.Trim(),
                    out var candidate))
            {
                throw new ItineraryOutputException(
                    $"The item '{item.Title}' does not reference a verified place.");
            }

            if (!PurposeMatchesCategory(
                    candidate.Purpose,
                    category))
            {
                throw new ItineraryOutputException(
                    $"The item '{item.Title}' uses a {candidate.Purpose.ToString().ToLowerInvariant()} place as {item.Category}.");
            }

            if (!string.Equals(
                    candidate.City,
                    assignedCity,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ItineraryOutputException(
                    $"Day {day.DayNumber} is assigned to {assignedCity}, but '{candidate.Place.Name}' belongs to {candidate.City}.");
            }

            if (category is
                    (ItineraryItemCategory.Activity or
                     ItineraryItemCategory.Meal) &&
                 !usedReferences.Add(candidate.Reference))
            {
                var hasUnusedAlternative = places.Any(alternative =>
                    alternative.Purpose == candidate.Purpose &&
                    string.Equals(
                        alternative.City,
                        assignedCity,
                        StringComparison.OrdinalIgnoreCase) &&
                    !usedReferences.Contains(
                        alternative.Reference));

                if (hasUnusedAlternative)
                {
                    throw new ItineraryOutputException(
                        $"The verified place '{candidate.Place.Name}' is repeated even though another verified option is available.");
                }
            }
        }
    }
}

    public static void ValidatePlanRules(
        IReadOnlyList<AiItineraryDay> days,
        GenerateItineraryPreferences preferences,
        decimal budgetAmount,
        IReadOnlyList<ItineraryPlaceCandidate> places,
        IReadOnlyList<ItineraryCityStop> cityPlan)
    {
        ValidateDailyStructure(
            days,
            places,
            cityPlan);
        ValidateMealFrequency(
            days,
            places,
            cityPlan);
        ValidateMealVariety(days);
        ValidateAccommodation(
            days,
            preferences,
            places,
            cityPlan);
        ValidateLanguage(days, preferences);
        ValidateKnownBudget(
            days,
            budgetAmount,
            places);
    }

    private static void ValidateDailyStructure(
        IReadOnlyList<AiItineraryDay> days,
        IReadOnlyList<ItineraryPlaceCandidate> places,
        IReadOnlyList<ItineraryCityStop> cityPlan)
    {
        foreach (var day in days)
        {
            var assignedCity =
                ItineraryCityPlanner.GetStopForDay(
                    cityPlan,
                    day.DayNumber).City;

            var activityCandidates = places.Count(candidate =>
                candidate.Purpose ==
                ItineraryPlacePurpose.Activity &&
                string.Equals(
                    candidate.City,
                    assignedCity,
                    StringComparison.OrdinalIgnoreCase));

            var activityCount = day.Items.Count(item =>
                    ItineraryItemInput.ParseCategory(
                        item.Category) ==
                    ItineraryItemCategory.Activity);

            if (activityCandidates > 0 &&
                activityCount < 1)
            {
                throw new ItineraryOutputException(
                    $"Day {day.DayNumber} must include at least one verified activity.");
            }

            TimeSpan? previousEnd = null;

            foreach (var item in day.Items)
            {
                if (ItineraryItemInput.ParseCategory(
                        item.Category) ==
                    ItineraryItemCategory.Accommodation)
                {
                    continue;
                }

                var start =
                    ItineraryItemInput.ParseTime(
                        item.StartTime);
                var end =
                    ItineraryItemInput.ParseTime(
                        item.EndTime);

                if (start is null || end is null)
                {
                    throw new ItineraryOutputException(
                        $"The item '{item.Title}' must have a practical start and end time.");
                }

                if (previousEnd is not null &&
                    start < previousEnd)
                {
                    throw new ItineraryOutputException(
                        $"Day {day.DayNumber} contains overlapping or out-of-order items.");
                }

                previousEnd = end;
            }
        }
    }

    private static bool PurposeMatchesCategory(
        ItineraryPlacePurpose purpose,
        ItineraryItemCategory category)
    {
        return purpose switch
        {
            ItineraryPlacePurpose.Meal =>
                category == ItineraryItemCategory.Meal,
            ItineraryPlacePurpose.Accommodation =>
                category == ItineraryItemCategory.Accommodation,
            _ => category == ItineraryItemCategory.Activity
        };
    }

    private static void ValidateMealFrequency(
        IReadOnlyList<AiItineraryDay> days,
        IReadOnlyList<ItineraryPlaceCandidate> places,
        IReadOnlyList<ItineraryCityStop> cityPlan)
    {
        foreach (var day in days)
        {
            var mealCount = day.Items.Count(item =>
                ItineraryItemInput.ParseCategory(
                    item.Category) ==
                ItineraryItemCategory.Meal);

            if (mealCount > 3)
            {
                throw new ItineraryOutputException(
                    $"Day {day.DayNumber} cannot contain more than breakfast, lunch and dinner.");
            }
        }
    }

    private static void ValidateMealVariety(
        IReadOnlyList<AiItineraryDay> days)
    {
        var usedSuggestions = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var meal in days
                     .SelectMany(day => day.Items)
                     .Where(item =>
                         ItineraryItemInput.ParseCategory(
                             item.Category) ==
                         ItineraryItemCategory.Meal))
        {
            var suggestion =
                GetMealSuggestionKey(meal.Title);

            if (!usedSuggestions.Add(suggestion))
            {
                throw new ItineraryOutputException(
                    $"The meal suggestion '{meal.Title}' is repeated. Every breakfast, lunch and dinner must feature a different local plate.");
            }
        }
    }

    internal static string GetMealSuggestionKey(
        string title)
    {
        var parts = title.Split(
            ['—', '-', ':'],
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);
        var plate = parts.Length > 1
            ? parts[^1]
            : title;

        return Regex.Replace(
                plate.ToLowerInvariant(),
                @"[^\p{L}\p{N}]+",
                " ")
            .Trim();
    }

    internal static void NormalizeChronologicalOrder(
        IReadOnlyList<AiItineraryDay> days)
    {
        foreach (var day in days)
        {
            day.Items = day.Items
                .OrderBy(item =>
                    ItineraryItemInput.ParseCategory(
                        item.Category) ==
                    ItineraryItemCategory.Accommodation
                        ? TimeSpan.MinValue
                        : ItineraryItemInput.ParseTime(
                            item.StartTime)
                          ?? TimeSpan.MaxValue)
                .ThenBy(item => item.OrderIndex)
                .ToList();

            for (var index = 0;
                 index < day.Items.Count;
                 index++)
            {
                day.Items[index].OrderIndex = index;
            }
        }
    }

    private static void ValidateAccommodation(
        IReadOnlyList<AiItineraryDay> days,
        GenerateItineraryPreferences preferences,
        IReadOnlyList<ItineraryPlaceCandidate> places,
        IReadOnlyList<ItineraryCityStop> cityPlan)
    {
        var accommodationItems = days
            .SelectMany(day => day.Items.Select(item =>
                (Day: day, Item: item)))
            .Where(entry =>
                ItineraryItemInput.ParseCategory(
                    entry.Item.Category) ==
                ItineraryItemCategory.Accommodation)
            .ToList();

        var alreadyBooked = string.Equals(
            preferences.AccommodationStyle,
            "already_booked",
            StringComparison.OrdinalIgnoreCase);

        if (alreadyBooked && accommodationItems.Count > 0)
        {
            throw new ItineraryOutputException(
                "Accommodation is already booked, so the itinerary must not recommend another hotel.");
        }

        foreach (var cityStop in cityPlan)
        {
            var cityAccommodationItems =
                accommodationItems
                    .Where(entry =>
                        entry.Day.DayNumber >=
                            cityStop.StartDayNumber &&
                        entry.Day.DayNumber <=
                            cityStop.EndDayNumber)
                    .ToList();

            if (cityAccommodationItems.Count > 1)
            {
                throw new ItineraryOutputException(
                    $"The itinerary must contain no more than one accommodation recommendation in {cityStop.City}.");
            }

            if (cityAccommodationItems.Any(entry =>
                    entry.Day.DayNumber !=
                    cityStop.StartDayNumber))
            {
                throw new ItineraryOutputException(
                    $"The accommodation recommendation for {cityStop.City} must appear on day {cityStop.StartDayNumber}.");
            }

            var hasVerifiedAccommodation =
                places.Any(candidate =>
                    candidate.Purpose ==
                        ItineraryPlacePurpose.Accommodation &&
                    string.Equals(
                        candidate.City,
                        cityStop.City,
                        StringComparison.OrdinalIgnoreCase));

            if (!alreadyBooked &&
                hasVerifiedAccommodation &&
                cityAccommodationItems.Count != 1)
            {
                throw new ItineraryOutputException(
                    $"A verified accommodation candidate is available in {cityStop.City} and must be included once on day {cityStop.StartDayNumber}.");
            }
        }

        foreach (var entry in accommodationItems)
        {
            if (ContainsAccommodationPriceClaim(
                    entry.Item.Title) ||
                ContainsAccommodationPriceClaim(
                    entry.Item.Description) ||
                ContainsAccommodationPriceClaim(
                    entry.Day.Notes))
            {
                throw new ItineraryOutputException(
                    "Accommodation text contains an unverified hotel price or nightly-rate claim.");
            }
        }
    }

    private static bool
        ContainsAccommodationPriceClaim(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return Regex.IsMatch(
            text,
            @"[$€£¥]|\b(?:USD|EUR|GBP|EGP|TRY|AED|SAR)\b|\bper\s+night\b|\bnightly\s+rate\b|\bprice\s+per\s+night\b|في\s+الليلة|سعر\s+الليلة",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);
    }

    private static void ValidateLanguage(
        IEnumerable<AiItineraryDay> days,
        GenerateItineraryPreferences preferences)
    {
        var visibleTexts = days.SelectMany(day =>
            new[]
            {
                day.Title,
                day.Notes
            }.Concat(day.Items.SelectMany(item =>
                new[]
                {
                    item.Title,
                    item.Description
                })))
            .Where(text =>
                !string.IsNullOrWhiteSpace(text))
            .Cast<string>()
            .ToList();

        var arabicRequested = string.Equals(
            preferences.PreferredLanguage,
            "ar",
            StringComparison.OrdinalIgnoreCase);

        if (arabicRequested)
        {
            var forbiddenPlanningWords =
                new HashSet<string>(
                [
                    "arrival",
                    "departure",
                    "day",
                    "morning",
                    "afternoon",
                    "evening",
                    "breakfast",
                    "lunch",
                    "dinner",
                    "transport",
                    "transfer",
                    "checkin",
                    "checkout"
                ],
                StringComparer.OrdinalIgnoreCase);

            foreach (var text in visibleTexts)
            {
                if (text.Any(char.IsLetter) &&
                    !ContainsArabic(text))
                {
                    throw new ItineraryOutputException(
                        "Arabic was requested, but some itinerary text is entirely in another language.");
                }

                var latinWords = Regex
                    .Matches(text, @"[A-Za-z]+")
                    .Select(match =>
                        match.Value
                            .Replace("-", string.Empty)
                            .ToLowerInvariant());

                if (latinWords.Any(
                        forbiddenPlanningWords.Contains))
                {
                    throw new ItineraryOutputException(
                        "Arabic itinerary text contains generic English planning words such as arrival, day or meal names.");
                }
            }

            return;
        }

        if (visibleTexts.Any(ContainsArabic))
        {
            throw new ItineraryOutputException(
                "English was requested, but the itinerary contains Arabic explanatory text.");
        }
    }

    private static bool ContainsArabic(string text)
    {
        return text.Any(character =>
            character is >= '\u0600' and <= '\u06FF'
            or >= '\u0750' and <= '\u077F'
            or >= '\u08A0' and <= '\u08FF'
            or >= '\uFB50' and <= '\uFDFF'
            or >= '\uFE70' and <= '\uFEFF');
    }

    private static void ValidateKnownBudget(
        IEnumerable<AiItineraryDay> days,
        decimal budgetAmount,
        IReadOnlyList<ItineraryPlaceCandidate> places)
    {
        var knownTotal = days
            .SelectMany(day => day.Items)
            .Sum(item =>
            {
                var category =
                    ItineraryItemInput.ParseCategory(
                        item.Category);

                if (category !=
                    ItineraryItemCategory.Accommodation)
                {
                    return item.EstimatedCost ?? 0;
                }

                return ItineraryPlaceCatalog.FindPlace(
                           places,
                           item.PlaceReference)
                       ?.TotalRate
                       ?? 0;
            });

        if (knownTotal > budgetAmount)
        {
            throw new ItineraryOutputException(
                $"The known itinerary estimate ({knownTotal:0.00}) exceeds the total budget ({budgetAmount:0.00}).");
        }
    }

    private static void ValidateDay(AiItineraryDay day)
    {
        if (day.DayNumber <= 0 || string.IsNullOrWhiteSpace(day.Date))
        {
            throw new ItineraryOutputException("A day number or date is missing.");
        }

        if (string.IsNullOrWhiteSpace(day.Title))
        {
            day.Title = $"Day {day.DayNumber}";
        }

        day.Items ??= new List<AiItineraryItem>();

        if (day.Items.Count == 0)
        {
            throw new ItineraryOutputException(
                $"Day {day.DayNumber} does not contain any itinerary items.");
        }

        for (var index = 0; index < day.Items.Count; index++)
        {
            var item = day.Items[index];

            // JSON array order is authoritative. Models occasionally repeat
            // or skip orderIndex values even when the item order is clear.
            item.OrderIndex = index;

            if (string.IsNullOrWhiteSpace(item.Title))
            {
                throw new ItineraryOutputException(
                    "An itinerary item title is missing.");
            }

            if (!ItineraryItemInput.IsValidCategory(
                    item.Category))
            {
                throw new ItineraryOutputException(
                    $"The category '{item.Category}' is not supported.");
            }

            if (!ItineraryItemInput.IsValidTime(
                    item.StartTime) ||
                !ItineraryItemInput.IsValidTime(
                    item.EndTime))
            {
                throw new ItineraryOutputException(
                    "Itinerary item times must use HH:mm.");
            }

            var startTime =
                ItineraryItemInput.ParseTime(item.StartTime);
            var endTime =
                ItineraryItemInput.ParseTime(item.EndTime);

            if (startTime is not null &&
                endTime is not null &&
                endTime <= startTime)
            {
                throw new ItineraryOutputException(
                    "An itinerary item must end after it starts.");
            }

            var category =
                ItineraryItemInput.ParseCategory(item.Category);

            if (category ==
                ItineraryItemCategory.Accommodation)
            {
                if (item.EstimatedCost is not null)
                {
                    throw new ItineraryOutputException(
                        "Accommodation estimatedCost must be null because live room rates are unavailable.");
                }

                continue;
            }

            if (item.EstimatedCost is null)
            {
                throw new ItineraryOutputException(
                    "Every itinerary item must contain an estimated cost.");
            }

            if (item.EstimatedCost < 0)
            {
                throw new ItineraryOutputException(
                    "Estimated cost cannot be negative.");
            }

            if (category is
                    ItineraryItemCategory.Meal or
                    ItineraryItemCategory.Transport &&
                item.EstimatedCost <= 0)
            {
                throw new ItineraryOutputException(
                    "Meal and transport costs must be greater than zero.");
            }
        }
    }

    private static IReadOnlyList<string> ExtractJsonObjects(
        string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            throw new ItineraryOutputException(
                "The AI returned an empty response.");
        }

        var jsonObjects = new List<string>();
        var objectStart = -1;
        var objectDepth = 0;
        var insideString = false;
        var escaped = false;

        for (var index = 0; index < output.Length; index++)
        {
            var character = output[index];

            if (objectStart < 0)
            {
                if (character == '{')
                {
                    objectStart = index;
                    objectDepth = 1;
                    insideString = false;
                    escaped = false;
                }

                continue;
            }

            if (insideString)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (character == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (character == '"')
                {
                    insideString = false;
                }

                continue;
            }

            if (character == '"')
            {
                insideString = true;
                continue;
            }

            if (character == '{')
            {
                objectDepth++;
                continue;
            }

            if (character != '}')
            {
                continue;
            }

            objectDepth--;

            if (objectDepth != 0)
            {
                continue;
            }

            jsonObjects.Add(
                output[objectStart..(index + 1)]);

            objectStart = -1;
        }

        if (objectStart >= 0)
        {
            throw new ItineraryOutputException(
                "The AI response ended inside an incomplete JSON object.");
        }

        if (jsonObjects.Count == 0)
        {
            throw new ItineraryOutputException(
                "The AI response did not contain any itinerary day objects.");
        }

        return jsonObjects;
    }
}
