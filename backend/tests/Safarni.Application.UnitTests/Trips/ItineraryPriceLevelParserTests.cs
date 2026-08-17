using FluentAssertions;
using Safarni.Application.Features.Trips.GenerateItinerary;

namespace Safarni.Application.UnitTests.Trips;

public sealed class ItineraryPriceLevelParserTests
{
    [Theory]
    [InlineData("E£200–1,200", "EGP", 200)]
    [InlineData("EGP 1,250 - 2,000", "EGP", 1250)]
    [InlineData("$25–50", "USD", 25)]
    [InlineData("€12.50–30", "EUR", 12.50)]
    public void TryGetMinimum_returns_first_live_amount(
        string priceLevel,
        string currency,
        decimal expected)
    {
        var found = ItineraryPriceLevelParser.TryGetMinimum(
            priceLevel,
            currency,
            out var minimum);

        found.Should().BeTrue();
        minimum.Should().Be(expected);
    }

    [Fact]
    public void TryGetMinimum_rejects_a_different_currency()
    {
        var found = ItineraryPriceLevelParser.TryGetMinimum(
            "E£200–1,200",
            "USD",
            out _);

        found.Should().BeFalse();
    }

    [Theory]
    [InlineData("$$")]
    [InlineData("")]
    public void TryGetMinimum_rejects_non_numeric_price_levels(string priceLevel)
    {
        ItineraryPriceLevelParser.TryGetMinimum(
                priceLevel,
                "USD",
                out _)
            .Should()
            .BeFalse();
    }
}
