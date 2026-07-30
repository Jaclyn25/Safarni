using System.Globalization;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

internal static class ItineraryItemInput
{
    public static bool IsValidCategory(string? value)
    {
        return Enum.TryParse<ItineraryItemCategory>(value, true, out _);
    }

    public static ItineraryItemCategory ParseCategory(string value)
    {
        return Enum.TryParse<ItineraryItemCategory>(value, true, out var category)
            ? category
            : ItineraryItemCategory.Other;
    }

    public static bool IsValidTime(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ||
               TimeSpan.TryParseExact(
                   value,
                   [
                       @"h\:mm",
                       @"hh\:mm"
                   ],
                   CultureInfo.InvariantCulture,
                   TimeSpanStyles.None,
                   out _);
    }

    public static TimeSpan? ParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return TimeSpan.ParseExact(
            value,
            [
                @"h\:mm",
                @"hh\:mm"
            ],
            CultureInfo.InvariantCulture,
            TimeSpanStyles.None);
    }
}
