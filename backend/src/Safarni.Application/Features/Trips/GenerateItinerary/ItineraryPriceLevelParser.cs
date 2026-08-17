using System.Globalization;
using System.Text.RegularExpressions;

namespace Safarni.Application.Features.Trips.GenerateItinerary;

internal static partial class ItineraryPriceLevelParser
{
    public static bool TryGetMinimum(
        string? priceLevel,
        string itineraryCurrencyCode,
        out decimal minimum)
    {
        minimum = 0m;
        if (string.IsNullOrWhiteSpace(priceLevel))
        {
            return false;
        }

        var detectedCurrency = DetectCurrency(priceLevel);
        if (detectedCurrency is not null &&
            !string.Equals(
                detectedCurrency,
                itineraryCurrencyCode,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var match = NumericAmount().Match(priceLevel);
        if (!match.Success)
        {
            return false;
        }

        var normalized = match.Value.Replace(",", string.Empty);
        return decimal.TryParse(
                   normalized,
                   NumberStyles.AllowDecimalPoint,
                   CultureInfo.InvariantCulture,
                   out minimum) &&
               minimum > 0m;
    }

    private static string? DetectCurrency(string value)
    {
        var normalized = value.ToUpperInvariant();
        if (normalized.Contains("EGP") || normalized.Contains("E£")) return "EGP";
        if (normalized.Contains("USD") || normalized.Contains("US$")) return "USD";
        if (normalized.Contains("EUR") || normalized.Contains('€')) return "EUR";
        if (normalized.Contains("GBP") || normalized.Contains('£')) return "GBP";
        if (normalized.Contains("TRY") || normalized.Contains('₺')) return "TRY";
        if (normalized.Contains("AED")) return "AED";
        if (normalized.Contains("SAR")) return "SAR";
        if (normalized.Contains("JPY") || normalized.Contains('¥')) return "JPY";
        if (normalized.Contains('$')) return "USD";
        return null;
    }

    [GeneratedRegex(@"\d[\d,]*(?:\.\d+)?", RegexOptions.CultureInvariant)]
    private static partial Regex NumericAmount();
}
