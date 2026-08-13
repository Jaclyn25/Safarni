using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using FluentAssertions;
using Moq;
using Safarni.Application.Abstractions.Travel;
using Safarni.Application.Features.Trips.GenerateItinerary;
using Safarni.Domain.Entities;

namespace Safarni.Application.UnitTests.Trips;

public sealed class ItineraryGenerationPipelineTests
{
    [Fact]
    public void ParseRange_ShouldPreserveArabicText_FromStructuredJson()
    {
        const string output = """
            {
              "days": [
                {
                  "dayNumber": 1,
                  "date": "2026-08-01",
                  "title": "استكشاف برشلونة",
                  "notes": "يوم ثقافي هادئ",
                  "items": [
                    {
                      "startTime": "09:00",
                      "endTime": "11:00",
                      "title": "زيارة الحي القوطي",
                      "description": "جولة بين المباني التاريخية والساحات القديمة.",
                      "location": "Barri Gòtic",
                      "placeReference": "P1",
                      "category": "activity",
                      "estimatedCost": 15,
                      "orderIndex": 0
                    }
                  ]
                },
                {
                  "dayNumber": 2,
                  "date": "2026-08-02",
                  "title": "معالم برشلونة",
                  "notes": null,
                  "items": [
                    {
                      "startTime": "10:00",
                      "endTime": "12:00",
                      "title": "جولة معمارية",
                      "description": "مشاهدة التفاصيل المعمارية المميزة.",
                      "location": "Sagrada Família",
                      "placeReference": "P2",
                      "category": "activity",
                      "estimatedCost": 30,
                      "orderIndex": 0
                    }
                  ]
                }
              ]
            }
            """;
        var trip = new Trip
        {
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 8, 2)
        };

        var days = ItineraryOutputParser.ParseRange(
            output,
            trip,
            1,
            2);

        days.Should().HaveCount(2);
        days[0].Title.Should().Be("استكشاف برشلونة");
        days[0].Items[0].Description.Should().Contain("التاريخية");
        days[1].Items[0].Location.Should().Be("Sagrada Família");
    }

    [Fact]
    public void ParseRange_ShouldStillAcceptLegacyEnglishNdjson()
    {
        const string output = """
            {"dayNumber":1,"date":"2026-08-01","title":"Barcelona highlights","notes":null,"items":[{"startTime":"09:00","endTime":"11:00","title":"Gothic Quarter walk","description":"Explore historic streets and squares.","location":"Barri Gòtic","placeReference":"P1","category":"activity","estimatedCost":15,"orderIndex":0}]}
            {"dayNumber":2,"date":"2026-08-02","title":"Barcelona architecture","notes":"Start early","items":[{"startTime":"10:00","endTime":"12:00","title":"Architecture tour","description":"See the city's distinctive architecture.","location":"Sagrada Família","placeReference":"P2","category":"activity","estimatedCost":30,"orderIndex":0}]}
            """;
        var trip = new Trip
        {
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 8, 2)
        };

        var days = ItineraryOutputParser.ParseRange(
            output,
            trip,
            1,
            2);

        days.Should().HaveCount(2);
        days[0].Title.Should().Be("Barcelona highlights");
        days[1].Items[0].Title.Should().Be("Architecture tour");
    }

    [Fact]
    public void BuildCompletionOptions_ShouldRequestStructuredItineraryJson()
    {
        var options =
            ItineraryGenerationProcessor.BuildCompletionOptions(3);

        options.ResponseFormat.Should().Be(
            AICompletionResponseFormat.ItineraryJson);
    }

    [Fact]
    public void ResolvePlaceReferences_ShouldReplaceWrongCityMealReference()
    {
        var cairoCafe = CreateMealCandidate(
            "P1",
            "EUGÉNIE CAFE",
            "Cairo",
            4.8);
        var hurghadaRestaurant = CreateMealCandidate(
            "P2",
            "Hurghada Star Restaurant",
            "Hurghada",
            4.7);
        var day = new AiItineraryDay
        {
            DayNumber = 3,
            Date = "2026-08-03",
            Title = "Hurghada arrival",
            Items =
            [
                new AiItineraryItem
                {
                    StartTime = "13:00",
                    EndTime = "14:00",
                    Title = "EUGÉNIE CAFE",
                    Description = "Enjoy lunch after arriving in Hurghada.",
                    Location = "EUGÉNIE CAFE",
                    PlaceReference = "P1",
                    Category = "meal",
                    EstimatedCost = 25m,
                    OrderIndex = 0
                }
            ]
        };
        var cityStop = new ItineraryCityStop(
            "Hurghada",
            3,
            5,
            3,
            null,
            null);

        ItineraryPlaceCatalog.ResolvePlaceReferences(
            day,
            [cairoCafe, hurghadaRestaurant],
            "Hurghada");

        day.Items[0].PlaceReference.Should().Be("P2");
        day.Items[0].Location.Should().Be("Hurghada Star Restaurant");
        day.Items[0].Title.Should().Be("Hurghada Star Restaurant");

        Action validate = () =>
            ItineraryOutputParser.ValidatePlaceReferences(
                [day],
                [cairoCafe, hurghadaRestaurant],
                [cityStop]);

        validate.Should().NotThrow();
    }

    [Fact]
    public void BuildPromptCatalog_ShouldGroupReferencesByCity()
    {
        var catalog = ItineraryPlaceCatalog.BuildPromptCatalog(
        [
            CreateMealCandidate("P1", "Cairo Cafe", "Cairo", 4.6),
            CreateMealCandidate(
                "P2",
                "Hurghada Restaurant",
                "Hurghada",
                4.7)
        ]);

        catalog.Should().Contain("CITY CATALOG: Cairo");
        catalog.Should().Contain("CITY CATALOG: Hurghada");
        catalog.IndexOf("CITY CATALOG: Cairo", StringComparison.Ordinal)
            .Should().BeLessThan(
                catalog.IndexOf(
                    "CITY CATALOG: Hurghada",
                    StringComparison.Ordinal));
    }

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
    public void EnsureMinimumVerifiedActivity_ShouldFillFreeWindow_WhenGeminiReturnsNoActivity()
    {
        var candidate = CreateCandidate(
            "P1",
            "Cairo Tower",
            "Cairo",
            photoUrl: "https://images.example/cairo-tower.jpg");
        var day = CreateDay(
            new AiItineraryItem
            {
                StartTime = "08:00",
                EndTime = "09:00",
                Title = "Breakfast",
                Description = "Breakfast at the hotel.",
                Location = "Cairo",
                Category = "meal",
                OrderIndex = 0
            },
            new AiItineraryItem
            {
                StartTime = "12:00",
                EndTime = "13:00",
                Title = "Lunch",
                Description = "Lunch break.",
                Location = "Cairo",
                Category = "meal",
                OrderIndex = 1
            });
        var cityStop = new ItineraryCityStop(
            "Cairo",
            1,
            1,
            0,
            null,
            null);

        var result =
            ItineraryMinimumActivityResolver.Ensure(
                [day],
                [candidate],
                cityStop,
                "en",
                flightPlan: null,
                totalTripDays: 1);

        result.Status.Should().Be(
            ItineraryMinimumActivityRepairStatus.Inserted);
        day.Items.Should().ContainSingle(item =>
            item.Category == "activity" &&
            item.PlaceReference == "P1" &&
            item.StartTime == "09:00" &&
            item.EndTime == "10:30");

        Action validate = () => ItineraryOutputParser.ValidatePlanRules(
            [day],
            CreatePreferences(),
            budgetAmount: null,
            [candidate],
            [cityStop]);

        validate.Should().NotThrow();
    }

    [Fact]
    public void EnsureMinimumVerifiedActivity_ShouldRespectArrivalWindow()
    {
        var candidate = CreateCandidate(
            "P1",
            "Cairo Tower",
            "Cairo",
            photoUrl: "https://images.example/cairo-tower.jpg");
        var day = CreateDay();
        var cityStop = new ItineraryCityStop(
            "Cairo",
            1,
            1,
            0,
            null,
            null);
        var flightPlan = new ItineraryFlightPlan(
            new FlightRecommendation(
                "CAI",
                "Cairo International Airport",
                "IST",
                "Istanbul Airport",
                "11:00",
                "14:00",
                "Test Air",
                "TA100",
                180,
                0,
                100m,
                "USD",
                null,
                "https://www.google.com/travel/flights"),
            null);

        var result =
            ItineraryMinimumActivityResolver.Ensure(
                [day],
                [candidate],
                cityStop,
                "en",
                flightPlan,
                totalTripDays: 1);

        result.Status.Should().Be(
            ItineraryMinimumActivityRepairStatus.Inserted);
        var activity = day.Items.Should().ContainSingle().Subject;
        var activityStart =
            ItineraryItemInput.ParseTime(activity.StartTime);
        activityStart.Should().NotBeNull();
        activityStart!.Value.Should().BeGreaterThanOrEqualTo(
            flightPlan.FirstDayAvailableFrom!.Value);
    }

    [Fact]
    public void EnsureMinimumVerifiedActivity_ShouldReclaimOtherTimeBeforeMeal()
    {
        var candidate = CreateCandidate(
            "P1",
            "Antalya Museum",
            "Antalya",
            photoUrl: "https://images.example/antalya-museum.jpg");
        var day = CreateDay(
            CreateScheduledItem(
                "Free time",
                "other",
                "08:00",
                "12:00"),
            CreateScheduledItem(
                "Lunch - piyaz",
                "meal",
                "12:00",
                "13:00"),
            CreateScheduledItem(
                "Personal time",
                "other",
                "13:00",
                "22:00"));
        var cityStop = CreateCityStop("Antalya");

        var result = ItineraryMinimumActivityResolver.Ensure(
            [day],
            [candidate],
            cityStop,
            "en",
            flightPlan: null,
            totalTripDays: 1);

        result.Status.Should().Be(
            ItineraryMinimumActivityRepairStatus.Inserted);
        result.RemovedItems.Should().ContainSingle();
        day.Items.Should().Contain(item =>
            item.Category == "meal" &&
            item.Title == "Lunch - piyaz");
        day.Items.Should().ContainSingle(item =>
            item.Category == "activity" &&
            item.PlaceReference == "P1");
        AssertChronologicalWithoutOverlap(day);
    }

    [Fact]
    public void EnsureMinimumVerifiedActivity_ShouldRemoveMeal_WhenNoLowerPriorityTimeExists()
    {
        var candidate = CreateCandidate(
            "P1",
            "Antalya Museum",
            "Antalya",
            photoUrl: "https://images.example/antalya-museum.jpg");
        var day = CreateDay(
            CreateScheduledItem(
                "Breakfast - gozleme",
                "meal",
                "08:00",
                "12:00"),
            CreateScheduledItem(
                "Lunch - piyaz",
                "meal",
                "12:00",
                "17:00"),
            CreateScheduledItem(
                "Dinner - kebab",
                "meal",
                "17:00",
                "22:00"));

        var result = ItineraryMinimumActivityResolver.Ensure(
            [day],
            [candidate],
            CreateCityStop("Antalya"),
            "en",
            flightPlan: null,
            totalTripDays: 1);

        result.Status.Should().Be(
            ItineraryMinimumActivityRepairStatus.Inserted);
        result.RemovedItems.Should().ContainSingle();
        day.Items.Count(item => item.Category == "meal")
            .Should().Be(2);
        day.Items.Should().ContainSingle(item =>
            item.Category == "activity");
        AssertChronologicalWithoutOverlap(day);
    }

    [Fact]
    public void MinimumActivityValidation_ShouldAllowProtectedFullDayTransport()
    {
        var candidate = CreateCandidate(
            "P1",
            "Antalya Museum",
            "Antalya",
            photoUrl: "https://images.example/antalya-museum.jpg");
        var day = CreateDay(
            new AiItineraryItem
            {
                StartTime = "08:00",
                EndTime = "22:00",
                Title = "Intercity transfer to Antalya",
                Description = "Travel to the next city.",
                Location = "Antalya",
                Category = "transport",
                EstimatedCost = 50m,
                OrderIndex = 0
            });
        var cityStop = CreateCityStop("Antalya");

        var result = ItineraryMinimumActivityResolver.Ensure(
            [day],
            [candidate],
            cityStop,
            "en",
            flightPlan: null,
            totalTripDays: 1);

        result.Status.Should().Be(
            ItineraryMinimumActivityRepairStatus.NoFeasibleWindow);

        Action validate = () => ItineraryOutputParser.ValidatePlanRules(
            [day],
            CreatePreferences(accommodation: "already_booked"),
            budgetAmount: null,
            [candidate],
            [cityStop],
            flightPlan: null,
            totalTripDays: 1);

        validate.Should().NotThrow();
    }

    [Fact]
    public void MinimumActivityValidation_ShouldUseFlightPlan_OnEarlyDepartureDay()
    {
        var candidate = CreateCandidate(
            "P1",
            "Antalya Museum",
            "Antalya",
            photoUrl: "https://images.example/antalya-museum.jpg");
        var day = CreateDay(
            CreateScheduledItem(
                "Airport preparation",
                "other",
                "04:30",
                "05:00"));
        var cityStop = CreateCityStop("Antalya");
        var flightPlan = new ItineraryFlightPlan(
            null,
            new FlightRecommendation(
                "AYT",
                "Antalya Airport",
                "CAI",
                "Cairo International Airport",
                "07:40",
                "09:00",
                "Test Air",
                "TA200",
                80,
                0,
                100m,
                "USD",
                null,
                "https://www.google.com/travel/flights"));

        var result = ItineraryMinimumActivityResolver.Ensure(
            [day],
            [candidate],
            cityStop,
            "en",
            flightPlan,
            totalTripDays: 1);

        result.Status.Should().Be(
            ItineraryMinimumActivityRepairStatus.NoEligibleDay);

        Action validate = () => ItineraryOutputParser.ValidatePlanRules(
            [day],
            CreatePreferences(accommodation: "already_booked"),
            budgetAmount: null,
            [candidate],
            [cityStop],
            flightPlan,
            totalTripDays: 1);

        validate.Should().NotThrow();
    }

    [Fact]
    public void EnsureMinimumVerifiedActivity_ShouldChooseDayWithoutRemovals()
    {
        var candidate = CreateCandidate(
            "P1",
            "Antalya Museum",
            "Antalya",
            photoUrl: "https://images.example/antalya-museum.jpg");
        var firstDay = CreateDay(
            CreateScheduledItem(
                "Free time",
                "other",
                "08:00",
                "22:00"));
        var secondDay = new AiItineraryDay
        {
            DayNumber = 2,
            Date = "2026-08-02",
            Title = "Antalya highlights",
            Items =
            [
                new AiItineraryItem
                {
                    StartTime = "08:00",
                    EndTime = "09:00",
                    Title = "Intercity arrival",
                    Description = "Travel into Antalya.",
                    Location = "Antalya",
                    Category = "transport",
                    EstimatedCost = 20m,
                    OrderIndex = 0
                }
            ]
        };
        var cityStop = new ItineraryCityStop(
            "Antalya",
            1,
            2,
            1,
            null,
            null);

        var result = ItineraryMinimumActivityResolver.Ensure(
            [firstDay, secondDay],
            [candidate],
            cityStop,
            "en",
            flightPlan: null,
            totalTripDays: 2);

        result.Status.Should().Be(
            ItineraryMinimumActivityRepairStatus.Inserted);
        result.DayNumber.Should().Be(2);
        result.RemovedItems.Should().BeEmpty();
        firstDay.Items.Should().NotContain(item =>
            item.Category == "activity");
        secondDay.Items.Should().ContainSingle(item =>
            item.Category == "activity");
        AssertChronologicalWithoutOverlap(secondDay);
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
    [InlineData(1, 3000)]
    [InlineData(2, 3800)]
    [InlineData(5, 6000)]
    [InlineData(6, 6000)]
    [InlineData(20, 6000)]
    public void CompletionTokens_ShouldScaleByFullTripDays(
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
    public async Task SerpPrimaryCatalog_ShouldUseThreeBoundedQueriesAndPreferUnusedPlaces()
    {
        var travel = new Mock<IItineraryTravelDataService>();
        travel.SetupGet(service => service.IsConfigured).Returns(true);
        travel
            .Setup(service => service.SearchLocalAsync(
                It.IsAny<string>(),
                "Cairo",
                "en",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                string query,
                string _,
                string _,
                CancellationToken _) =>
            {
                if (query.Contains(
                        "restaurants",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Enumerable.Range(1, 6)
                        .Select(index => new PlaceDetails
                        {
                            PlaceId = $"meal-{index}",
                            Name = $"Cairo Restaurant {index}",
                            Address = $"Cairo Restaurant {index}, Cairo",
                            PhotoUrl = $"https://images.example/meal-{index}.jpg",
                            Rating = 4.5,
                            UserRatingsTotal = 100 + index,
                            Types = ["restaurant"]
                        })
                        .ToList();
                }

                return Enumerable.Range(1, 8)
                    .Select(index => new PlaceDetails
                    {
                        PlaceId = index == 1
                            ? "old-activity"
                            : $"activity-{index}",
                        Name = $"Cairo Attraction {index}",
                        Address = $"Cairo Attraction {index}, Cairo",
                        PhotoUrl = $"https://images.example/activity-{index}.jpg",
                        Rating = 4.7,
                        UserRatingsTotal = 500 + index,
                        Types = ["tourist_attraction"]
                    })
                    .ToList();
            });
        travel
            .Setup(service => service.SearchHotelsAsync(
                "Cairo",
                "mid_range",
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                2,
                "USD",
                "en",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new PlaceDetails
                {
                    PlaceId = "hotel-1",
                    Name = "Cairo Hotel",
                    Address = "Cairo Hotel, Cairo",
                    PhotoUrl = "https://images.example/hotel.jpg",
                    Rating = 4.4,
                    Types = ["hotel", "lodging"]
                }
            ]);

        var trip = new Trip
        {
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 8, 2)
        };
        var catalog = await ItineraryPlaceCatalog.LoadSerpPrimaryAsync(
            travel.Object,
            trip,
            [new ItineraryCityStop(
                "Cairo",
                1,
                2,
                1,
                null,
                null)],
            CreatePreferences(),
            "USD",
            2,
            new HashSet<string>(
                ["old-activity"],
                StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        catalog.Should().Contain(candidate =>
            candidate.Purpose == ItineraryPlacePurpose.Activity);
        catalog.Should().Contain(candidate =>
            candidate.Purpose == ItineraryPlacePurpose.Meal);
        catalog.Should().Contain(candidate =>
            candidate.Purpose == ItineraryPlacePurpose.Accommodation);
        catalog.Should().OnlyContain(candidate =>
            !string.IsNullOrWhiteSpace(candidate.Place.PhotoUrl));
        catalog.Should().NotContain(candidate =>
            candidate.Place.PlaceId == "old-activity");
        travel.Verify(service => service.SearchLocalAsync(
            It.IsAny<string>(),
            "Cairo",
            "en",
            It.IsAny<CancellationToken>()), Times.Exactly(3));
        travel.Verify(service => service.SearchHotelsAsync(
            "Cairo",
            "mid_range",
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            2,
            "USD",
            "en",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void PromptCatalog_ShouldExposeCoordinatesAndCoarseAreaCluster()
    {
        var candidate = CreateCandidate(
            "P1",
            "Cairo Tower",
            "Cairo",
            "https://images.example/cairo-tower.jpg");
        candidate.Place.Latitude = 30.04592;
        candidate.Place.Longitude = 31.22429;

        var catalog = ItineraryPlaceCatalog.BuildPromptCatalog(
            [candidate]);

        catalog.Should().Contain("coordinates:30.04592,31.22429");
        catalog.Should().Contain("areaCluster:30.05,31.22");
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

    [Theory]
    [InlineData("Evening Stroll", "Take an easy stroll near the hotel.")]
    [InlineData("Relax at the Resort", "Relax and enjoy some unplanned time.")]
    public void ReconcileActivityItems_ShouldKeepGenericLeisureAsOther(
        string title,
        string description)
    {
        var day = CreateDay(
            new AiItineraryItem
            {
                StartTime = "19:00",
                EndTime = "20:00",
                Title = title,
                Description = description,
                Location = "Hotel",
                PlaceReference = null,
                Category = "other",
                EstimatedCost = 0m,
                OrderIndex = 0
            });

        var result = ItineraryPlaceCatalog.ReconcileActivityItems(
            [day],
            [],
            "Hurghada",
            "en");

        result.RepairedCount.Should().Be(0);
        result.RemovedCount.Should().Be(0);
        day.Items.Should().ContainSingle();
        day.Items[0].Category.Should().Be("other");

        Action validate = () =>
            ItineraryPlaceCatalog.ValidateRemainingOtherItems([day]);

        validate.Should().NotThrow();
    }

    [Fact]
    public void ReconcileActivityItems_ShouldRepairEveryNonLeisureOtherItem()
    {
        var verifiedActivity = CreateCandidate(
            "P1",
            "Hurghada Marina",
            "Hurghada",
            photoUrl: "https://images.example/hurghada-marina.jpg");
        var day = CreateDay(
            new AiItineraryItem
            {
                StartTime = "17:00",
                EndTime = "18:30",
                Title = "Sunset Experience",
                Description = "Enjoy the destination before dinner.",
                Location = "Hurghada",
                PlaceReference = null,
                Category = "other",
                EstimatedCost = 10m,
                OrderIndex = 0
            });

        var result = ItineraryPlaceCatalog.ReconcileActivityItems(
            [day],
            [verifiedActivity],
            "Hurghada",
            "en");

        result.RepairedCount.Should().Be(1);
        result.RemovedCount.Should().Be(0);
        day.Items[0].Category.Should().Be("activity");
        day.Items[0].PlaceReference.Should().Be("P1");

        Action validate = () =>
            ItineraryPlaceCatalog.ValidateRemainingOtherItems([day]);

        validate.Should().NotThrow();
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

    private static ItineraryCityStop CreateCityStop(string city)
    {
        return new ItineraryCityStop(
            city,
            1,
            1,
            0,
            null,
            null);
    }

    private static AiItineraryItem CreateScheduledItem(
        string title,
        string category,
        string start,
        string end)
    {
        return new AiItineraryItem
        {
            StartTime = start,
            EndTime = end,
            Title = title,
            Description = "Generated schedule item.",
            Location = "Antalya",
            Category = category,
            EstimatedCost = category == "meal" ? 10m : 0m,
            OrderIndex = 0
        };
    }

    private static void AssertChronologicalWithoutOverlap(
        AiItineraryDay day)
    {
        TimeSpan? previousEnd = null;

        foreach (var item in day.Items)
        {
            var start = ItineraryItemInput.ParseTime(item.StartTime);
            var end = ItineraryItemInput.ParseTime(item.EndTime);

            start.Should().NotBeNull();
            end.Should().NotBeNull();
            end!.Value.Should().BeGreaterThan(start!.Value);

            if (previousEnd is not null)
            {
                start.Value.Should().BeGreaterThanOrEqualTo(
                    previousEnd.Value);
            }

            previousEnd = end.Value;
        }
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

    private static ItineraryPlaceCandidate CreateMealCandidate(
        string reference,
        string name,
        string city,
        double rating)
    {
        return new ItineraryPlaceCandidate(
            reference,
            new PlaceDetails
            {
                PlaceId = $"meal-{reference}",
                Name = name,
                Address = $"{name}, {city}",
                Rating = rating,
                UserRatingsTotal = 300,
                PhotoUrl = $"https://images.example/{reference}.jpg",
                Types = ["restaurant", "food"]
            },
            ItineraryPlacePurpose.Meal,
            city,
            null);
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
