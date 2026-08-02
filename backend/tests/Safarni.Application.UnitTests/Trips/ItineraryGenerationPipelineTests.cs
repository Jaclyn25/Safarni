using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Travel;
using Safarni.Application.Features.Trips.GenerateItinerary;

namespace Safarni.Application.UnitTests.Trips;

public sealed class ItineraryGenerationPipelineTests
{
    [Fact]
    public void ReconcileActivityItems_ShouldGroundUnverifiedActivity_ToUnusedVerifiedCandidate()
    {
        var tower = CreateCandidate(
            "P1",
            "Cairo Tower",
            "Cairo",
            photoUrl: "https://images.example/cairo-tower.jpg");
        var museum = CreateCandidate(
            "P2",
            "The Egyptian Museum",
            "Cairo",
            photoUrl: "https://images.example/egyptian-museum.jpg");
        var day = CreateDay(
            CreateActivity(
                "Cairo Tower",
                "P1",
                "Cairo Tower",
                "09:00",
                "10:00"),
            CreateActivity(
                "Adventure Park",
                null,
                "Adventure Park",
                "11:00",
                "12:00"));

        var result = ItineraryPlaceCatalog.ReconcileActivityItems(
            [day],
            [tower, museum],
            "Cairo",
            "en");

        result.RepairedCount.Should().Be(1);
        result.RemovedCount.Should().Be(0);
        day.Items[1].PlaceReference.Should().Be("P2");
        day.Items[1].Location.Should().Be("The Egyptian Museum");
        day.Items[1].Title.Should().Be("The Egyptian Museum");

        Action validate = () =>
            ItineraryOutputParser.ValidatePlaceReferences(
                [day],
                [tower, museum],
                [new ItineraryCityStop(
                    "Cairo",
                    1,
                    1,
                    0,
                    null,
                    null)]);

        validate.Should().NotThrow();
    }

    [Fact]
    public void ReconcileActivityItems_ShouldRemoveExtraUnverifiedActivity_WhenCatalogIsExhausted()
    {
        var candidate = CreateCandidate(
            "P1",
            "Cairo Tower",
            "Cairo",
            photoUrl: "https://images.example/cairo-tower.jpg");
        var day = CreateDay(
            CreateActivity(
                "Cairo Tower",
                "P1",
                "Cairo Tower",
                "09:00",
                "10:00"),
            CreateActivity(
                "Adventure Park",
                null,
                "Adventure Park",
                "11:00",
                "12:00"));

        var result = ItineraryPlaceCatalog.ReconcileActivityItems(
            [day],
            [candidate],
            "Cairo",
            "en");

        result.RepairedCount.Should().Be(0);
        result.RemovedCount.Should().Be(1);
        day.Items.Should().ContainSingle();
        day.Items[0].PlaceReference.Should().Be("P1");
    }


    [Fact]
    public void ReconcileActivityItems_ShouldNotStealReferenceReservedByLaterValidItem()
    {
        var candidate = CreateCandidate(
            "P1",
            "Cairo Tower",
            "Cairo",
            photoUrl: "https://images.example/cairo-tower.jpg");
        var day = CreateDay(
            CreateActivity(
                "Adventure Park",
                null,
                "Adventure Park",
                "09:00",
                "10:00"),
            CreateActivity(
                "Cairo Tower",
                "P1",
                "Cairo Tower",
                "11:00",
                "12:00"));

        var result = ItineraryPlaceCatalog.ReconcileActivityItems(
            [day],
            [candidate],
            "Cairo",
            "en");

        result.RepairedCount.Should().Be(0);
        result.RemovedCount.Should().Be(1);
        day.Items.Should().ContainSingle();
        day.Items[0].PlaceReference.Should().Be("P1");
        day.Items[0].Location.Should().Be("Cairo Tower");
    }

    [Fact]
    public async Task VerifyActivitySuggestions_ShouldUseGoogleFallback_WhenTravelResultIsUnrelated()
    {
        var google = new Mock<IGooglePlacesService>();
        google
            .Setup(service => service.TextSearchAsync(
                It.Is<string>(query => query == "Cairo"),
                It.IsAny<CancellationToken>(),
                "en"))
            .ReturnsAsync(new List<PlaceDetails>());
        google
            .Setup(service => service.TextSearchAsync(
                It.Is<string>(query =>
                    query.Contains("Cairo Tower")),
                It.IsAny<CancellationToken>(),
                "en"))
            .ReturnsAsync(
                new List<PlaceDetails>
                {
                    new()
                    {
                    PlaceId = "google-cairo-tower",
                    Name = "Cairo Tower",
                    Address = "Zamalek, Cairo, Egypt",
                    Rating = 4.5,
                    UserRatingsTotal = 1000,
                    PhotoUrl = "https://images.example/cairo-tower.jpg",
                    Types = ["tourist_attraction"]
                    }
                });

        var travel = new Mock<IItineraryTravelDataService>();
        travel.SetupGet(service => service.IsConfigured).Returns(true);
        travel
            .Setup(service => service.SearchLocalAsync(
                It.IsAny<string>(),
                "Cairo",
                "en",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new List<PlaceDetails>
                {
                    new()
                    {
                    PlaceId = "unrelated",
                    Name = "Unrelated Adventure Park",
                    Address = "Cairo, Egypt",
                    PhotoUrl = "https://images.example/unrelated.jpg",
                    Types = ["tourist_attraction"]
                    }
                });

        var verified = await ItineraryPlaceCatalog
            .VerifyActivitySuggestionsAsync(
                google.Object,
                travel.Object,
                [new ItineraryActivitySuggestion(
                    "Cairo Tower",
                    "Cairo",
                    "landmark")],
                [new ItineraryCityStop(
                    "Cairo",
                    1,
                    1,
                    0,
                    null,
                    null)],
                "en",
                CancellationToken.None);

        verified.Should().ContainSingle();
        verified[0].Place.PlaceId.Should().Be("google-cairo-tower");
        verified[0].Place.PhotoUrl.Should().NotBeNullOrWhiteSpace();
        google.Verify(service => service.TextSearchAsync(
            It.Is<string>(query => query.Contains("Cairo Tower")),
            It.IsAny<CancellationToken>(),
            "en"), Times.Once);
    }

    [Fact]
    public void EntityFactory_ShouldAttachVerifiedActivityPhotoAndPlaceMetadata()
    {
        var candidate = CreateCandidate(
            "P1",
            "Cairo Tower",
            "Cairo",
            photoUrl: "https://images.example/cairo-tower.jpg");
        var generatedDay = CreateDay(
            CreateActivity(
                "Cairo Tower",
                "P1",
                "Cairo Tower",
                "09:00",
                "10:00"));

        var savedDay = ItineraryEntityFactory.CreateDay(
            Guid.NewGuid(),
            generatedDay,
            [candidate]);

        savedDay.Items.Should().ContainSingle();
        var savedItem = savedDay.Items.Single();
        savedItem.GooglePlaceId.Should().Be("place-P1");
        savedItem.PhotoUrl.Should().Be(
            "https://images.example/cairo-tower.jpg");
        savedItem.Address.Should().Be(
            "Cairo Tower, Cairo");
    }

    [Fact]
    public void ReconcileActivityItems_ShouldRemoveUnsupportedVenue_WhenNoApiCandidateExists()
    {
        var meal = new AiItineraryItem
        {
            StartTime = "12:00",
            EndTime = "13:00",
            Title = "Lunch — koshari",
            Description = "Try a local plate.",
            Location = "Cairo",
            Category = "meal",
            EstimatedCost = 15m,
            OrderIndex = 0
        };
        var unsupported = CreateActivity(
            "Imaginary Adventure Park",
            null,
            "Imaginary Adventure Park",
            "14:00",
            "16:00");
        var day = CreateDay(meal, unsupported);

        var result = ItineraryPlaceCatalog.ReconcileActivityItems(
            [day],
            [],
            "Cairo",
            "en");

        result.RepairedCount.Should().Be(0);
        result.RemovedCount.Should().Be(1);
        day.Items.Should().ContainSingle();
        day.Items[0].Category.Should().Be("meal");
    }

    [Fact]
    public void ReconcileActivityItems_ShouldKeepOfficialArabicNameOnlyInLocation_ForEnglishOutput()
    {
        var candidate = CreateCandidate(
            "P1",
            "المتحف المصري",
            "Cairo",
            photoUrl: "https://images.example/museum.jpg");
        var day = CreateDay(
            CreateActivity(
                "Museum visit",
                null,
                "Museum",
                "09:00",
                "11:00"));

        ItineraryPlaceCatalog.ReconcileActivityItems(
            [day],
            [candidate],
            "Cairo",
            "en");

        day.Items[0].Title.Should().Be("Visit verified activity");
        day.Items[0].Location.Should().Be("المتحف المصري");

        Action validateLanguage = () =>
            ItineraryOutputParser.ValidateLanguage(
                [day],
                CreatePreferences(language: "en"));

        validateLanguage.Should().NotThrow();
    }

    [Fact]
    public void ValidateLanguage_ShouldReportTheExactRejectedField()
    {
        var day = CreateDay(
            new AiItineraryItem
            {
                StartTime = "09:00",
                EndTime = "10:00",
                Title = "زيارة المتحف",
                Description = "English description.",
                Location = "المتحف المصري",
                Category = "other",
                EstimatedCost = 0m,
                OrderIndex = 0
            });

        Action validate = () =>
            ItineraryOutputParser.ValidateLanguage(
                [day],
                CreatePreferences(language: "en"));

        validate.Should()
            .Throw<ItineraryOutputException>()
            .WithMessage("*day 1, item 1 title*زيارة المتحف*");
    }


    [Fact]
    public void LanguageNormalizer_ShouldRepairArabicExplanatoryText_ForEnglishOutput()
    {
        var day = CreateDay(
            new AiItineraryItem
            {
                StartTime = "12:00",
                EndTime = "13:00",
                Title = "غداء محلي",
                Description = "تجربة طبق محلي.",
                Location = "Cairo",
                Category = "meal",
                EstimatedCost = 15m,
                OrderIndex = 0
            });
        day.Title = "يوم في القاهرة";
        day.Notes = "ملاحظات عربية";

        var adjustments = ItineraryLanguageNormalizer.Normalize(
            [day],
            "en");

        adjustments.Should().Be(4);
        day.Title.Should().Be("Day 1 itinerary");
        day.Notes.Should().BeNull();
        day.Items[0].Title.Should().Be(
            "Lunch — local specialty for day 1");
        day.Items[0].Description.Should().NotContain("تجربة");

        Action validate = () =>
            ItineraryOutputParser.ValidateLanguage(
                [day],
                CreatePreferences(language: "en"));

        validate.Should().NotThrow();
    }
    [Theory]
    [InlineData(1, 1400)]
    [InlineData(2, 1800)]
    [InlineData(5, 3000)]
    [InlineData(6, 3200)]
    [InlineData(20, 3200)]
    public void CompletionTokens_ShouldScaleByCitySegmentDays(
        int dayCount,
        int expected)
    {
        ItineraryGenerationProcessor
            .BuildCompletionOptions(dayCount)
            .MaxTokens
            .Should()
            .Be(expected);
    }

    [Fact]
    public void FlightWindow_ShouldNotRequireActivity_OnEarlyDepartureDay()
    {
        ItineraryOutputParser.HasUsableActivityWindow(
                dayNumber: 5,
                totalTripDays: 5,
                firstDayAvailableFrom: null,
                lastDayMustEndBy: new TimeSpan(5, 40, 0))
            .Should()
            .BeFalse();
    }

    [Fact]
    public void FlightWindow_ShouldNotRequireActivity_AfterLateArrival()
    {
        ItineraryOutputParser.HasUsableActivityWindow(
                dayNumber: 1,
                totalTripDays: 5,
                firstDayAvailableFrom: new TimeSpan(21, 0, 0),
                lastDayMustEndBy: null)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void FlightWindow_ShouldRequireActivity_OnOrdinaryDay()
    {
        ItineraryOutputParser.HasUsableActivityWindow(
                dayNumber: 2,
                totalTripDays: 5,
                firstDayAvailableFrom: null,
                lastDayMustEndBy: null)
            .Should()
            .BeTrue();
    }


    [Fact]
    public void FlightWindowResolver_ShouldReplaceLateFinalDayItems_WithSafeDepartureItem()
    {
        var day = CreateDay(
            new AiItineraryItem
            {
                StartTime = "12:00",
                EndTime = "13:00",
                Title = "Lunch — try koshari",
                Description = "Try a local plate.",
                Location = "Cairo",
                Category = "meal",
                EstimatedCost = 15m,
                OrderIndex = 0
            });
        var flightPlan = new ItineraryFlightPlan(
            null,
            new FlightRecommendation(
                "CAI",
                "Cairo International Airport",
                "SYD",
                "Sydney Airport",
                "07:40",
                "21:00",
                "Test Airline",
                "TA200",
                800,
                0,
                600m,
                "USD",
                null,
                "https://flights.example"));

        var adjustments = ItineraryFlightWindowResolver.Normalize(
            [day],
            "Cairo",
            "en",
            flightPlan,
            totalTripDays: 1);

        adjustments.Should().Be(2);
        day.Items.Should().ContainSingle();
        day.Items[0].Title.Should().Be("Airport preparation");
        var endTime = ItineraryItemInput.ParseTime(
            day.Items[0].EndTime);
        endTime.Should().NotBeNull();
        endTime!.Value.Should().BeLessThanOrEqualTo(
            new TimeSpan(5, 40, 0));
    }

    [Fact]
    public void FlightWindowResolver_ShouldRemoveItemsBeforeVerifiedArrival()
    {
        var day = CreateDay(
            new AiItineraryItem
            {
                StartTime = "08:00",
                EndTime = "09:00",
                Title = "Breakfast — try ful medames",
                Description = "Try a local breakfast.",
                Location = "Cairo",
                Category = "meal",
                EstimatedCost = 10m,
                OrderIndex = 0
            });
        var flightPlan = new ItineraryFlightPlan(
            new FlightRecommendation(
                "SYD",
                "Sydney Airport",
                "CAI",
                "Cairo International Airport",
                "08:00",
                "19:00",
                "Test Airline",
                "TA100",
                660,
                0,
                500m,
                "USD",
                null,
                "https://flights.example"),
            null);

        ItineraryFlightWindowResolver.Normalize(
            [day],
            "Cairo",
            "en",
            flightPlan,
            totalTripDays: 2);

        day.Items.Should().ContainSingle();
        day.Items[0].Title.Should().Be("Arrival and settle in");
        day.Items[0].StartTime.Should().Be("20:30");
    }

    [Fact]
    public void AccommodationResolver_ShouldPlaceCheckInAfterVerifiedLateArrival()
    {
        var hotel = CreateHotelCandidate(
            "P1",
            "Verified Cairo Hotel",
            "Cairo",
            4.5,
            200m);
        var day = CreateDay(
            new AiItineraryItem
            {
                StartTime = "21:00",
                EndTime = "22:00",
                Title = "Dinner — try koshari",
                Description = "Try a local plate.",
                Location = "Cairo",
                Category = "meal",
                EstimatedCost = 15m,
                OrderIndex = 0
            });
        var flightPlan = new ItineraryFlightPlan(
            new FlightRecommendation(
                "SYD",
                "Sydney Airport",
                "CAI",
                "Cairo International Airport",
                "08:00",
                "19:00",
                "Test Airline",
                "TA100",
                660,
                0,
                500m,
                "USD",
                null,
                "https://flights.example"),
            null);

        ItineraryAccommodationResolver.NormalizeCityStay(
            [day],
            new ItineraryCityStop(
                "Cairo",
                1,
                2,
                1,
                "CAI",
                "Cairo International Airport"),
            CreatePreferences(),
            [hotel],
            flightPlan,
            totalTripDays: 2);

        var accommodation = day.Items.Single(item =>
            item.Category == "accommodation");
        accommodation.StartTime.Should().Be("20:30");
        accommodation.EndTime.Should().Be("20:45");
        accommodation.PlaceReference.Should().Be("P1");
    }

    [Fact]
    public void AccommodationResolver_ShouldKeepItemsInChronologicalOrder()
    {
        var hotel = CreateHotelCandidate(
            "P1",
            "Verified Cairo Hotel",
            "Cairo",
            4.5,
            200m);
        var day = CreateDay(
            new AiItineraryItem
            {
                StartTime = "08:00",
                EndTime = "09:00",
                Title = "Breakfast — try ful medames",
                Description = "Try a local breakfast.",
                Location = "Cairo",
                Category = "meal",
                EstimatedCost = 10m,
                OrderIndex = 0
            },
            CreateActivity(
                "Cairo Tower",
                "P2",
                "Cairo Tower",
                "10:00",
                "12:00"));

        ItineraryAccommodationResolver.NormalizeCityStay(
            [day],
            new ItineraryCityStop(
                "Cairo",
                1,
                2,
                1,
                null,
                null),
            CreatePreferences(),
            [hotel]);

        day.Items.Select(item => item.StartTime).Should().ContainInOrder(
            "08:00",
            "10:00",
            "15:00");
        day.Items.Select(item => item.OrderIndex).Should().Equal(0, 1, 2);
    }


    [Fact]
    public void AccommodationResolver_ShouldMoveCheckInAfterConflictingActivity()
    {
        var hotel = CreateHotelCandidate(
            "P1",
            "Verified Cairo Hotel",
            "Cairo",
            4.5,
            200m);
        var day = CreateDay(
            CreateActivity(
                "Cairo Tower",
                "P2",
                "Cairo Tower",
                "14:00",
                "16:00"));

        ItineraryAccommodationResolver.NormalizeCityStay(
            [day],
            new ItineraryCityStop(
                "Cairo",
                1,
                2,
                1,
                null,
                null),
            CreatePreferences(),
            [hotel]);

        var accommodation = day.Items.Single(item =>
            item.Category == "accommodation");
        accommodation.StartTime.Should().Be("16:00");
        accommodation.EndTime.Should().Be("16:15");
        day.Items.Select(item => item.StartTime).Should().ContainInOrder(
            "14:00",
            "16:00");
    }
    [Fact]
    public void AccommodationResolver_ShouldRemoveHotel_WhenCityHasNoHotelNight()
    {
        var hotel = CreateHotelCandidate(
            "P1",
            "Verified Cairo Hotel",
            "Cairo",
            4.5,
            200m);
        var existingHotelItem = new AiItineraryItem
        {
            StartTime = "15:00",
            EndTime = "15:15",
            Title = "Verified Cairo Hotel",
            Description = "Recommended stay for this city.",
            Location = "Verified Cairo Hotel",
            PlaceReference = "P1",
            Category = "accommodation",
            EstimatedCost = null,
            OrderIndex = 0
        };
        var day = CreateDay(existingHotelItem);

        ItineraryAccommodationResolver.NormalizeCityStay(
            [day],
            new ItineraryCityStop(
                "Cairo",
                1,
                1,
                0,
                null,
                null),
            CreatePreferences(),
            [hotel]);

        day.Items.Should().BeEmpty();
    }

    [Fact]
    public void PlannerPrompt_ShouldPreferFewerActivitiesOverInventedVenues()
    {
        ItineraryPromptBuilder.SystemPrompt.Should().Contain(
            "use fewer activities. Never invent a venue");
    }

    [Theory]
    [InlineData("Check-out")]
    [InlineData("Checkout")]
    [InlineData("Hotel check-out")]
    [InlineData("Check-in")]
    [InlineData("Hotel check-in")]
    public void ValidateRemainingOtherItems_ShouldAllowAdministrativeCheckItems(
        string title)
    {
        var day = CreateDay(
            new AiItineraryItem
            {
                StartTime = "05:00",
                EndTime = "05:20",
                Title = title,
                Description = "Prepare to leave the hotel.",
                Location = "Hotel",
                Category = "other",
                EstimatedCost = 0m,
                OrderIndex = 0
            });

        Action validate = () =>
            ItineraryPlaceCatalog.ValidateRemainingOtherItems([day]);

        validate.Should().NotThrow();
        day.Items[0].PlaceReference.Should().BeNull();
    }

    [Fact]
    public void ActivityDiscovery_ShouldDiscardGenericSuggestion()
    {
        const string output = """
            {"name":"Adventure Park","city":"Cairo","kind":"adventure"}
            {"name":"Cairo Tower","city":"Cairo","kind":"landmark"}
            """;
        var cityPlan = new[]
        {
            new ItineraryCityStop(
                "Cairo",
                1,
                1,
                0,
                null,
                null)
        };

        var suggestions = ItineraryActivityDiscovery.Parse(
            output,
            cityPlan,
            CreatePreferences());

        suggestions.Should().ContainSingle();
        suggestions[0].Name.Should().Be("Cairo Tower");
    }

    [Fact]
    public void HotelOrdering_ShouldUseAccommodationPreference()
    {
        var cityPlan = new[]
        {
            new ItineraryCityStop(
                "Cairo",
                1,
                2,
                1,
                null,
                null)
        };
        var fallback = new[]
        {
            CreateHotelCandidate(
                "P1",
                "Budget Hotel",
                "Cairo",
                3.5,
                80m),
            CreateHotelCandidate(
                "P2",
                "Luxury Hotel",
                "Cairo",
                4.9,
                500m),
            CreateHotelCandidate(
                "P3",
                "Mid Hotel",
                "Cairo",
                4.3,
                180m)
        };

        var luxury = ItineraryPlaceCatalog.MergeSuggestedActivities(
            fallback,
            [],
            cityPlan,
            CreatePreferences(accommodation: "luxury"));
        var budget = ItineraryPlaceCatalog.MergeSuggestedActivities(
            fallback,
            [],
            cityPlan,
            CreatePreferences(accommodation: "budget"));

        luxury.First(item =>
                item.Purpose ==
                ItineraryPlacePurpose.Accommodation)
            .Place.Name.Should().Be("Luxury Hotel");
        budget.First(item =>
                item.Purpose ==
                ItineraryPlacePurpose.Accommodation)
            .Place.Name.Should().Be("Budget Hotel");
    }

    private static AiItineraryDay CreateDay(
        params AiItineraryItem[] items)
    {
        return new AiItineraryDay
        {
            DayNumber = 1,
            Date = "2026-08-01",
            Title = "Cairo highlights",
            Notes = "Verified plan",
            Items = items.ToList()
        };
    }

    private static AiItineraryItem CreateActivity(
        string title,
        string? reference,
        string location,
        string start,
        string end)
    {
        return new AiItineraryItem
        {
            StartTime = start,
            EndTime = end,
            Title = title,
            Description = "A verified travel experience.",
            Location = location,
            PlaceReference = reference,
            Category = "activity",
            EstimatedCost = 10m,
            OrderIndex = 0
        };
    }

    private static ItineraryPlaceCandidate CreateCandidate(
        string reference,
        string name,
        string city,
        string? photoUrl)
    {
        return new ItineraryPlaceCandidate(
            reference,
            new PlaceDetails
            {
                PlaceId = $"place-{reference}",
                Name = name,
                Address = $"{name}, {city}",
                Rating = 4.6,
                UserRatingsTotal = 500,
                PhotoUrl = photoUrl,
                Types = ["tourist_attraction"]
            },
            ItineraryPlacePurpose.Activity,
            city,
            null);
    }

    private static ItineraryPlaceCandidate CreateHotelCandidate(
        string reference,
        string name,
        string city,
        double rating,
        decimal totalRate)
    {
        return new ItineraryPlaceCandidate(
            reference,
            new PlaceDetails
            {
                PlaceId = $"hotel-{reference}",
                Name = name,
                Address = $"{name}, {city}",
                Rating = rating,
                UserRatingsTotal = 200,
                TotalRate = totalRate,
                PriceCurrency = "USD",
                Types = ["hotel", "lodging"]
            },
            ItineraryPlacePurpose.Accommodation,
            city,
            1);
    }

    private static GenerateItineraryPreferences CreatePreferences(
        string language = "en",
        string accommodation = "mid_range")
    {
        return new GenerateItineraryPreferences
        {
            Interests = ["culture", "adventure"],
            Pace = "balanced",
            Transportation = ["walking"],
            AccommodationStyle = accommodation,
            PreferredLanguage = language
        };
    }
}
