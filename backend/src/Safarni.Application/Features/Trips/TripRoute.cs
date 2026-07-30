using System.Text.Json;

namespace Safarni.Application.Features.Trips;

public sealed record TripCitySelection(
    string Name,
    int Days);

internal static class TripRouteSerializer
{
    private static readonly JsonSerializerOptions Options =
        new(JsonSerializerDefaults.Web);

    public static string Serialize(
        IReadOnlyList<TripCitySelection>? cities)
    {
        return JsonSerializer.Serialize(
            cities ?? [],
            Options);
    }

    public static IReadOnlyList<TripCitySelection> Deserialize(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<
                       List<TripCitySelection>>(
                       value,
                       Options)
                   ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static int GetTripDayCount(
        DateTime startDate,
        DateTime endDate)
    {
        return Math.Max(
            1,
            (endDate.Date - startDate.Date).Days + 1);
    }
}
