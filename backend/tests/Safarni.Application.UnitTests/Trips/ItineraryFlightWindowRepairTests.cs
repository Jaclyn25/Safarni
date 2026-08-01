using FluentAssertions;
using Safarni.Application.Features.Trips.GenerateItinerary;

namespace Safarni.Application.UnitTests.Trips;

public sealed class ItineraryFlightWindowRepairTests
{
    [Fact]
    public void Repair_ShouldMoveAccommodationAfterArrivalWindow()
    {
        var day = CreateDay(
            1,
            CreateItem(
                "CityHub Otel & Hostel",
                "accommodation",
                "14:00",
                "15:00"));

        var changes = ItineraryFlightWindowRepair.Repair(
            [day],
            new TimeSpan(22, 10, 0),
            null,
            3);

        changes.Should().Be(1);
        day.Items.Should().ContainSingle();
        day.Items[0].StartTime.Should().Be("22:10");
        ItineraryItemInput.ParseTime(day.Items[0].EndTime)
            .Should().BeGreaterThan(new TimeSpan(22, 10, 0));
    }

    [Fact]
    public void Repair_ShouldRemoveActivityBeforeArrivalWindow()
    {
        var day = CreateDay(
            1,
            CreateItem(
                "Museum visit",
                "activity",
                "16:00",
                "18:00"),
            CreateItem(
                "Hotel check-in",
                "other",
                "21:00",
                "21:30"));

        var changes = ItineraryFlightWindowRepair.Repair(
            [day],
            new TimeSpan(22, 10, 0),
            null,
            3);

        changes.Should().Be(2);
        day.Items.Should().ContainSingle();
        day.Items[0].Title.Should().Be("Hotel check-in");
        day.Items[0].StartTime.Should().Be("22:10");
    }

    [Fact]
    public void Repair_ShouldRemoveMealAfterDepartureCutoff()
    {
        var day = CreateDay(
            5,
            CreateItem(
                "Check-out",
                "other",
                "04:30",
                "05:00"),
            CreateItem(
                "Lunch",
                "meal",
                "12:00",
                "13:00"));

        var changes = ItineraryFlightWindowRepair.Repair(
            [day],
            null,
            new TimeSpan(5, 40, 0),
            5);

        changes.Should().Be(1);
        day.Items.Should().ContainSingle();
        day.Items[0].Title.Should().Be("Check-out");
    }

    [Fact]
    public void Repair_ShouldLeaveValidItemsUnchanged()
    {
        var day = CreateDay(
            1,
            CreateItem(
                "Hotel check-in",
                "other",
                "22:30",
                "23:00"));

        var changes = ItineraryFlightWindowRepair.Repair(
            [day],
            new TimeSpan(22, 10, 0),
            null,
            3);

        changes.Should().Be(0);
        day.Items[0].StartTime.Should().Be("22:30");
        day.Items[0].EndTime.Should().Be("23:00");
    }

    private static AiItineraryDay CreateDay(
        int dayNumber,
        params AiItineraryItem[] items)
    {
        return new AiItineraryDay
        {
            DayNumber = dayNumber,
            Date = $"2026-08-{dayNumber:00}",
            Title = $"Day {dayNumber}",
            Items = items.ToList()
        };
    }

    private static AiItineraryItem CreateItem(
        string title,
        string category,
        string start,
        string end)
    {
        return new AiItineraryItem
        {
            Title = title,
            Category = category,
            StartTime = start,
            EndTime = end,
            EstimatedCost = category == "accommodation"
                ? null
                : 0m
        };
    }
}