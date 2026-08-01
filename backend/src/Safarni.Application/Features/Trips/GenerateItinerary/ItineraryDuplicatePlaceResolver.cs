using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

internal static class ItineraryDuplicatePlaceResolver
{
    public static int ReplaceDuplicates(
        IReadOnlyList<AiItineraryDay> days,
        string city,
        string languageCode,
        IReadOnlyList<ItineraryPlaceCandidate> places)
    {
        var candidatesByReference = places.ToDictionary(
            place => place.Reference,
            StringComparer.OrdinalIgnoreCase);
        var usedReferences = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        var replacements = 0;

        foreach (var item in days
                     .OrderBy(day => day.DayNumber)
                     .SelectMany(day => day.Items
                         .OrderBy(entry => entry.OrderIndex)))
        {
            var category =
                ItineraryItemInput.ParseCategory(item.Category);

            if (category is not
                    (ItineraryItemCategory.Activity or
                     ItineraryItemCategory.Meal) ||
                string.IsNullOrWhiteSpace(item.PlaceReference) ||
                !candidatesByReference.TryGetValue(
                    item.PlaceReference.Trim(),
                    out var currentPlace))
            {
                continue;
            }

            if (usedReferences.Add(currentPlace.Reference))
            {
                continue;
            }

            var replacement = places
                .Where(candidate =>
                    candidate.Purpose == currentPlace.Purpose &&
                    string.Equals(
                        candidate.City,
                        city,
                        StringComparison.OrdinalIgnoreCase) &&
                    !usedReferences.Contains(candidate.Reference))
                .OrderByDescending(candidate =>
                    candidate.Place.Rating ?? 0)
                .ThenByDescending(candidate =>
                    candidate.Place.UserRatingsTotal ?? 0)
                .FirstOrDefault();

            if (replacement is null)
            {
                continue;
            }

            item.PlaceReference = replacement.Reference;
            item.Location = replacement.Place.Name;
            item.Title = category == ItineraryItemCategory.Meal
                ? replacement.Place.Name
                : ItineraryTextPolicy.BuildActivityTitle(
                    replacement.Place,
                    languageCode);
            item.Description =
                ItineraryTextPolicy.BuildDuplicateReplacementDescription(
                    category == ItineraryItemCategory.Meal,
                    languageCode);

            usedReferences.Add(replacement.Reference);
            replacements++;
        }

        return replacements;
    }

}
