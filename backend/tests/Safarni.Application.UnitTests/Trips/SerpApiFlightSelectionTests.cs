using System.Text.Json;
using Safarni.Infrastructure.Travel;

namespace Safarni.Application.UnitTests.Trips;

public sealed class SerpApiFlightSelectionTests
{
    [Fact]
    public void SelectMatchingFlightResult_PrefersFirstGoogleBestFlight()
    {
        using var document = ParseResults(
            bestFlights:
            [
                Candidate("LXR", "SSH", 11311, 465),
                Candidate("LXR", "SSH", 13775, 240)
            ],
            otherFlights:
            [
                Candidate("LXR", "SSH", 10553, 790)
            ]);

        var selected = SerpApiFlightAirportService.SelectMatchingFlightResult(
            document.RootElement,
            "LXR",
            "SSH");

        Assert.NotNull(selected);
        Assert.Equal(465, selected.Value.GetProperty("total_duration").GetInt32());
        Assert.Equal(11311, selected.Value.GetProperty("price").GetInt32());
    }

    [Fact]
    public void SelectMatchingFlightResult_RejectsDominatedLongFallback()
    {
        using var document = ParseResults(
            bestFlights: [],
            otherFlights:
            [
                Candidate("LXR", "SSH", 13775, 790),
                Candidate("LXR", "SSH", 13775, 240),
                Candidate("LXR", "SSH", 14275, 200)
            ]);

        var selected = SerpApiFlightAirportService.SelectMatchingFlightResult(
            document.RootElement,
            "LXR",
            "SSH");

        Assert.NotNull(selected);
        Assert.Equal(240, selected.Value.GetProperty("total_duration").GetInt32());
    }

    [Fact]
    public void SelectMatchingFlightResult_IgnoresDifferentAirportRoute()
    {
        using var document = ParseResults(
            bestFlights:
            [
                Candidate("CAI", "SSH", 5000, 80),
                Candidate("LXR", "SSH", 11311, 465)
            ],
            otherFlights: []);

        var selected = SerpApiFlightAirportService.SelectMatchingFlightResult(
            document.RootElement,
            "LXR",
            "SSH");

        Assert.NotNull(selected);
        Assert.Equal(11311, selected.Value.GetProperty("price").GetInt32());
    }

    private static JsonDocument ParseResults(
        string[] bestFlights,
        string[] otherFlights) =>
        JsonDocument.Parse(
            $$"""
            {
              "best_flights": [{{string.Join(',', bestFlights)}}],
              "other_flights": [{{string.Join(',', otherFlights)}}]
            }
            """);

    private static string Candidate(
        string departure,
        string arrival,
        int price,
        int duration) =>
        $$"""
        {
          "price": {{price}},
          "total_duration": {{duration}},
          "flights": [
            {
              "departure_airport": { "id": "{{departure}}" },
              "arrival_airport": { "id": "{{arrival}}" }
            }
          ]
        }
        """;
}
