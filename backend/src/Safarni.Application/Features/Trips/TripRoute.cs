using System.Text.Json;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips;

public sealed record TripCitySelection(
    string Name,
    int Days,
    string? AirportCode = null,
    string? AirportName = null);

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

    /// <summary>
    /// Compatibility-only parser for legacy rows. New records use TripCities.
    /// Corrupt JSON is not silently accepted by generation code; use ReadFromTrip.
    /// </summary>
    public static IReadOnlyList<TripCitySelection> Deserialize(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<TripCitySelection>>(
                       value,
                       Options)
                   ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static IReadOnlyList<TripCitySelection> ReadFromTrip(Trip trip)
    {
        IReadOnlyList<TripCitySelection> route;

        if (trip.Cities.Count > 0)
        {
            var orderedRows = trip.Cities
                .OrderBy(city => city.OrderIndex)
                .ToList();

            if (orderedRows
                .Select((city, index) => city.OrderIndex == index)
                .Any(isContiguous => !isContiguous))
            {
                throw CorruptRoute(trip.Id);
            }

            route = orderedRows
                .Select(city => new TripCitySelection(
                    city.Name,
                    city.Days,
                    city.AirportCode,
                    city.AirportName))
                .ToList();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(trip.SelectedCitiesJson))
            {
                return [];
            }

            try
            {
                route =
                    JsonSerializer.Deserialize<List<TripCitySelection>>(
                        trip.SelectedCitiesJson,
                        Options)
                    ?? [];
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException(
                    $"Trip {trip.Id} contains corrupt city-route data.",
                    exception);
            }
        }

        ValidateRoute(trip, route);
        return route;
    }

    private static void ValidateRoute(
        Trip trip,
        IReadOnlyList<TripCitySelection> route)
    {
        var expectedDays = GetTripDayCount(
            trip.StartDate,
            trip.EndDate);

        var hasInvalidCity = route.Count == 0 ||
            route.Any(city =>
                string.IsNullOrWhiteSpace(city.Name) ||
                city.Days <= 0);

        var hasDuplicateCity = route
            .Select(city => city.Name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() != route.Count;

        if (hasInvalidCity ||
            hasDuplicateCity ||
            route.Sum(city => city.Days) != expectedDays)
        {
            throw CorruptRoute(trip.Id);
        }
    }

    private static InvalidOperationException CorruptRoute(Guid tripId)
    {
        return new InvalidOperationException(
            $"Trip {tripId} contains an invalid city route.");
    }

    public static void ReplaceCities(
        Trip trip,
        IReadOnlyList<TripCitySelection> selections)
    {
        trip.Cities.Clear();

        var normalized = selections
            .Select((city, index) => new TripCitySelection(
                city.Name.Trim(),
                city.Days,
                NormalizeAirportCode(city.AirportCode),
                NullIfWhiteSpace(city.AirportName)))
            .ToList();

        foreach (var (selection, index) in normalized.Select(
                     (selection, index) => (selection, index)))
        {
            trip.Cities.Add(new TripCity
            {
                Id = Guid.NewGuid(),
                TripId = trip.Id,
                Name = selection.Name,
                Days = selection.Days,
                OrderIndex = index,
                AirportCode = selection.AirportCode,
                AirportName = selection.AirportName
            });
        }

        // Keep a transition copy for old deployments/clients. It is no longer
        // the primary source of truth.
        trip.SelectedCitiesJson = Serialize(normalized);
    }

    public static int GetTripDayCount(
        DateTime startDate,
        DateTime endDate)
    {
        return Math.Max(
            1,
            (endDate.Date - startDate.Date).Days + 1);
    }

    public static string? NormalizeAirportCode(string? value)
    {
        var normalized = NullIfWhiteSpace(value)?.ToUpperInvariant();
        return normalized is { Length: 3 } && normalized.All(char.IsLetter)
            ? normalized
            : null;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
