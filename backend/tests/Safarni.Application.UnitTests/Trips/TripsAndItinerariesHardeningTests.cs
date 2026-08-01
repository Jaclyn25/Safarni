using FluentAssertions;
using Safarni.Application.Features.Trips;
using Safarni.Application.Features.Trips.CreateTrip;
using Safarni.Application.Features.Trips.GenerateItinerary;
using Safarni.Application.Features.Trips.Itineraries.ManageItems;

namespace Safarni.Application.UnitTests.Trips;

public sealed class TripsAndItinerariesHardeningTests
{
    [Fact]
    public void CreateTripValidator_ShouldAcceptExactOriginAndDestinationAirports()
    {
        var start = DateTime.UtcNow.Date.AddDays(5);
        var command = new CreateTripCommand(
            "Red Sea trip",
            "Egypt",
            start,
            start.AddDays(5),
            "Turkey",
            [
                new TripCitySelection(
                    "Hurghada",
                    3,
                    "HRG",
                    "Hurghada International Airport"),
                new TripCitySelection(
                    "Dahab",
                    3,
                    "SSH",
                    "Sharm El Sheikh International Airport")
            ],
            "TR",
            "Istanbul",
            "IST",
            "Istanbul Airport");

        var result = new CreateTripCommandValidator().Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateTripValidator_ShouldRejectMalformedAirportCodes()
    {
        var date = DateTime.UtcNow.Date.AddDays(5);
        var command = new CreateTripCommand(
            "Red Sea trip",
            "Egypt",
            date,
            date,
            "Turkey",
            [new TripCitySelection("Hurghada", 1, "HURGHADA")],
            "TUR",
            "Istanbul",
            "ISTANBUL");

        var result = new CreateTripCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == "OriginCountryCode");
        result.Errors.Should().Contain(error =>
            error.PropertyName == "OriginAirportCode");
        result.Errors.Should().Contain(error =>
            error.PropertyName == "Cities[0].AirportCode");
    }

    [Fact]
    public void AddItemValidator_ShouldRejectEndTimeBeforeStartTime()
    {
        var command = new AddItineraryItemCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Late activity",
            null,
            "18:00",
            "17:00",
            "Hurghada",
            "activity",
            25m,
            0);

        var result = new AddItineraryItemCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "End time must be later than start time.");
    }

    [Fact]
    public void UpdateItemValidator_ShouldAcceptMissingTimesButRejectEqualTimes()
    {
        var validator = new UpdateItineraryItemCommandValidator();

        var withoutTimes = validator.Validate(
            new UpdateItineraryItemCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Flexible item",
                null,
                null,
                null,
                "Dahab",
                "other",
                null));

        var equalTimes = validator.Validate(
            new UpdateItineraryItemCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Invalid item",
                null,
                "12:00",
                "12:00",
                "Dahab",
                "activity",
                10m));

        withoutTimes.IsValid.Should().BeTrue();
        equalTimes.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GenerationValidator_ShouldAllowNoBudget()
    {
        var command = new GenerateItineraryCommand(
            Guid.NewGuid(),
            new GenerateItineraryPreferences
            {
                Interests = ["culture", "food"],
                Pace = "balanced",
                Transportation = ["walking", "public_transport"],
                AccommodationStyle = "mid_range",
                PreferredLanguage = "en"
            },
            null,
            "USD",
            2);

        var result = new GenerateItineraryCommandValidator().Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
