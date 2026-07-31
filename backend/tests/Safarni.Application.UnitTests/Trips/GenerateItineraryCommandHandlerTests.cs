using ChatBotApi.Application.Common.DTOs;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Common.Models;
using ChatBotApi.Application.Common.Models.GooglePlaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Safarni.Application.Abstractions.Authentication;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Travel;
using Safarni.Application.Features.Trips.GenerateItinerary;
using Safarni.Domain.Entities;
using Xunit;

namespace Safarni.Application.UnitTests.Trips;

public class GenerateItineraryCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_SaveStructuredDaysAndItems_WhenAiOutputIsValid()
    {
        var userId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Rome Trip",
            Destination = "Rome",
            StartDate = new DateTime(2026, 9, 1),
            EndDate = new DateTime(2026, 9, 2),
            CreatedAt = DateTime.UtcNow
        };

        var currentUser =
            new Mock<ICurrentUserService>();

        var tripRepository =
            new Mock<ITripRepository>();

        var itineraryRepository =
            new Mock<IItineraryRepository>();

        var aiProvider =
            new Mock<IAIProvider>();

        var googlePlacesService =
            new Mock<IGooglePlacesService>();

        var logger =
            new Mock<
                ILogger<
                    GenerateItineraryCommandHandler>>();

        Itinerary? savedItinerary = null;

        currentUser
            .Setup(service => service.UserId)
            .Returns(userId);

        tripRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    trip.Id,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(trip);

        itineraryRepository
            .Setup(repository =>
                repository.AddAsync(
                    It.IsAny<Itinerary>(),
                    It.IsAny<CancellationToken>()))
            .Callback<Itinerary, CancellationToken>(
                (itinerary, _) =>
                    savedItinerary = itinerary)
            .Returns(Task.CompletedTask);

        googlePlacesService
            .Setup(service =>
                service.TextSearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<string>()))
            .ReturnsAsync(
                new List<PlaceDetails>
                {
                    new()
                    {
                        PlaceId = "google-place-colosseum",
                        Name = "Colosseum",
                        Address = "Piazza del Colosseo, Rome",
                        Rating = 4.7,
                        GoogleMapsLink =
                            "https://maps.google.com/?q=Colosseum",
                        Types = new List<string>
                        {
                            "tourist_attraction"
                        }
                    }
                });

        aiProvider
            .Setup(provider =>
                provider.GetChatCompletionAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<ChatMessageDto>>(),
                    It.IsAny<string>(),
                    It.IsAny<AICompletionOptions>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                """
                ```json
                [
                  {
                    "dayNumber": 1,
                    "date": "2026-09-01",
                    "title": "Ancient Rome",
                    "notes": null,
                    "items": [
                      {
                        "startTime": "9:00",
                        "endTime": "11:00",
                        "title": "Visit the Colosseum",
                        "description": "Visit the historic amphitheater.",
                        "location": "Colosseum",
                        "placeReference": "P1",
                        "category": "activity",
                        "estimatedCost": 25,
                        "orderIndex": 4
                      },
                      {
                        "startTime": "11:30",
                        "endTime": "12:00",
                        "title": "Travel to the next district",
                        "description": "Use public transport for the short transfer.",
                        "location": "Rome",
                        "placeReference": null,
                        "category": "transport",
                        "estimatedCost": 10,
                        "orderIndex": 9
                      }
                    ]
                  },
                  {
                    "dayNumber": 2,
                    "date": "2026-09-02",
                    "title": "Departure",
                    "notes": null,
                    "items": [
                      {
                        "startTime": "08:00",
                        "endTime": "09:00",
                        "title": "Prepare for departure",
                        "description": "Allow time to collect luggage and check out.",
                        "location": "Rome",
                        "placeReference": null,
                        "category": "transport",
                        "estimatedCost": 10,
                        "orderIndex": 3
                      },
                      {
                        "startTime": "09:30",
                        "endTime": "10:30",
                        "title": "Morning walk around the Colosseum",
                        "description": "Enjoy a final look at the historic amphitheater.",
                        "location": "Colosseum",
                        "placeReference": "P1",
                        "category": "activity",
                        "estimatedCost": 25,
                        "orderIndex": 5
                      },
                      {
                        "startTime": "11:00",
                        "endTime": "12:00",
                        "title": "Transfer to the airport",
                        "description": "Allow enough time for the airport transfer.",
                        "location": "Rome",
                        "placeReference": null,
                        "category": "transport",
                        "estimatedCost": 20,
                        "orderIndex": 7
                      }
                    ]
                  }
                ]
                ```
                """);

        var handler =
            new GenerateItineraryCommandHandler(
                currentUser.Object,
                tripRepository.Object,
                itineraryRepository.Object,
                aiProvider.Object,
                googlePlacesService.Object,
                logger.Object);

        var command =
            new GenerateItineraryCommand(
                trip.Id,
                CreatePreferences(),
                1000m,
                "USD",
                1);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        result.Value!.Days.Should().HaveCount(2);

        result.Value.Days[0]
            .Items.Should().HaveCount(2);

        result.Value.EstimatedTotalCost
            .Should().Be(90m);

        result.Value.Preferences.Interests
            .Should().BeEquivalentTo(
                new[]
                {
                    "culture",
                    "food"
                });

        savedItinerary.Should().NotBeNull();

        savedItinerary!.Days
            .Should().HaveCount(2);

        savedItinerary.BudgetAmount
            .Should().Be(1000m);

        savedItinerary.TravelerCount
            .Should().Be(1);

        savedItinerary.Preferences
            .Should().Contain(
                "\"preferredLanguage\":\"en\"");

        savedItinerary.Preferences
            .Should().Contain(
                "\"qualityVersion\":2");

        itineraryRepository.Verify(
            repository =>
                repository.SaveAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_FilterWrongDestination_AndKeepMealsApiSelectable()
    {
        var userId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Istanbul Trip",
            Destination = "Istanbul",
            StartDate = new DateTime(2026, 10, 1),
            EndDate = new DateTime(2026, 10, 1),
            CreatedAt = DateTime.UtcNow
        };

        var currentUser =
            new Mock<ICurrentUserService>();
        var tripRepository =
            new Mock<ITripRepository>();
        var itineraryRepository =
            new Mock<IItineraryRepository>();
        var aiProvider =
            new Mock<IAIProvider>();
        var googlePlacesService =
            new Mock<IGooglePlacesService>();
        var logger =
            new Mock<
                ILogger<
                    GenerateItineraryCommandHandler>>();

        Itinerary? savedItinerary = null;
        var sentPrompts = new List<string>();

        currentUser
            .Setup(service => service.UserId)
            .Returns(userId);

        tripRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    trip.Id,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(trip);

        itineraryRepository
            .Setup(repository =>
                repository.AddAsync(
                    It.IsAny<Itinerary>(),
                    It.IsAny<CancellationToken>()))
            .Callback<Itinerary, CancellationToken>(
                (itinerary, _) =>
                    savedItinerary = itinerary)
            .Returns(Task.CompletedTask);

        googlePlacesService
            .Setup(service =>
                service.TextSearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<string>()))
            .ReturnsAsync((
                string query,
                CancellationToken _,
                string _) =>
            {
                if (string.Equals(
                        query,
                        "Istanbul",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return
                    [
                        new PlaceDetails
                        {
                            PlaceId =
                                "istanbul-locality",
                            Name = "Istanbul",
                            Address =
                                "Istanbul, Türkiye",
                            Types = ["locality"]
                        }
                    ];
                }

                return
                [
                    new PlaceDetails
                    {
                        PlaceId =
                            "istanbul-museum",
                        Name = "Istanbul Museum",
                        Address =
                            "Fatih, Istanbul, Türkiye",
                        Rating = 4.7,
                        Types =
                            ["tourist_attraction"]
                    },
                    new PlaceDetails
                    {
                        PlaceId =
                            "istanbul-restaurant-one",
                        Name =
                            "Istanbul Local Table",
                        Address =
                            "Beyoğlu, Istanbul, Türkiye",
                        Rating = 4.6,
                        Types = ["restaurant"]
                    },
                    new PlaceDetails
                    {
                        PlaceId =
                            "istanbul-restaurant-two",
                        Name =
                            "Bosphorus Kitchen",
                        Address =
                            "Beşiktaş, Istanbul, Türkiye",
                        Rating = 4.5,
                        Types = ["restaurant"]
                    },
                    new PlaceDetails
                    {
                        PlaceId =
                            "rixos-belek",
                        Name =
                            "Rixos Premium Belek",
                        Address =
                            "Belek, Antalya, Türkiye",
                        Rating = 4.8,
                        Types = ["lodging"]
                    }
                ];
            });

        var outputs = new Queue<string>(
        [
            """
            {"dayNumber":1,"date":"2026-10-01","title":"Istanbul highlights","notes":null,"items":[{"startTime":"09:00","endTime":"11:00","title":"Visit Istanbul Museum","description":"Discover local history.","location":"Istanbul Museum","placeReference":"P1","category":"activity","estimatedCost":20,"orderIndex":0},{"startTime":"12:00","endTime":"13:00","title":"Lunch at Istanbul Local Table","description":"Try a local dish.","location":"Istanbul Local Table","placeReference":"P2","category":"meal","estimatedCost":30,"orderIndex":1},{"startTime":"19:00","endTime":"20:00","title":"Dinner at Istanbul Local Table","description":"Return for dinner.","location":"Istanbul Local Table","placeReference":"P2","category":"meal","estimatedCost":35,"orderIndex":2}]}
            """,
            """
            {"dayNumber":1,"date":"2026-10-01","title":"Istanbul highlights","notes":"A focused day with comfortable breaks.","items":[{"startTime":"09:00","endTime":"11:00","title":"Visit Istanbul Museum","description":"Discover local history.","location":"Istanbul Museum","placeReference":"P1","category":"activity","estimatedCost":20,"orderIndex":0},{"startTime":"13:00","endTime":"14:00","title":"Lunch at Istanbul Local Table","description":"Try a local dish.","location":"Istanbul Local Table","placeReference":"P2","category":"meal","estimatedCost":30,"orderIndex":1}]}
            """
        ]);

        aiProvider
            .Setup(provider =>
                provider.GetChatCompletionAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<ChatMessageDto>>(),
                    It.IsAny<string>(),
                    It.IsAny<AICompletionOptions>(),
                    It.IsAny<CancellationToken>()))
            .Callback<string, List<ChatMessageDto>, string, AICompletionOptions, CancellationToken>(
                (_, _, prompt, _, _) =>
                    sentPrompts.Add(prompt))
            .ReturnsAsync(() => outputs.Dequeue());

        var preferences = CreatePreferences() with
        {
            AccommodationStyle = "already_booked"
        };

        var handler =
            new GenerateItineraryCommandHandler(
                currentUser.Object,
                tripRepository.Object,
                itineraryRepository.Object,
                aiProvider.Object,
                googlePlacesService.Object,
                logger.Object);

        var result = await handler.Handle(
            new GenerateItineraryCommand(
                trip.Id,
                preferences,
                500m,
                "USD",
                1),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sentPrompts.Should().HaveCount(1);

        sentPrompts[0]
            .Should().NotContain(
                "Rixos Premium Belek");

        sentPrompts[0]
            .Should().Contain(
                "address:Fatih, Istanbul, Türkiye");

        savedItinerary.Should().NotBeNull();

        savedItinerary!.Days
            .SelectMany(day => day.Items)
            .Count(item =>
                item.Category ==
                ItineraryItemCategory.Meal)
            .Should().Be(2);

        aiProvider.Verify(
            provider =>
                provider.GetChatCompletionAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<ChatMessageDto>>(),
                    It.IsAny<string>(),
                    It.IsAny<AICompletionOptions>(),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_RetryMixedArabicAndLeaveHotelRateUnpriced()
    {
        var userId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "رحلة إسطنبول",
            Destination = "Istanbul",
            StartDate = new DateTime(2026, 11, 1),
            EndDate = new DateTime(2026, 11, 1),
            CreatedAt = DateTime.UtcNow
        };

        var currentUser =
            new Mock<ICurrentUserService>();
        var tripRepository =
            new Mock<ITripRepository>();
        var itineraryRepository =
            new Mock<IItineraryRepository>();
        var aiProvider =
            new Mock<IAIProvider>();
        var googlePlacesService =
            new Mock<IGooglePlacesService>();
        var logger =
            new Mock<
                ILogger<
                    GenerateItineraryCommandHandler>>();

        Itinerary? savedItinerary = null;
        var sentPrompts = new List<string>();

        currentUser
            .Setup(service => service.UserId)
            .Returns(userId);

        tripRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    trip.Id,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(trip);

        itineraryRepository
            .Setup(repository =>
                repository.AddAsync(
                    It.IsAny<Itinerary>(),
                    It.IsAny<CancellationToken>()))
            .Callback<Itinerary, CancellationToken>(
                (itinerary, _) =>
                    savedItinerary = itinerary)
            .Returns(Task.CompletedTask);

        googlePlacesService
            .Setup(service =>
                service.TextSearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<string>()))
            .ReturnsAsync((
                string query,
                CancellationToken _,
                string _) =>
            {
                if (string.Equals(
                        query,
                        "Istanbul",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return
                    [
                        new PlaceDetails
                        {
                            PlaceId =
                                "istanbul-locality",
                            Name = "Istanbul",
                            Address =
                                "Istanbul, Türkiye",
                            Types = ["locality"]
                        }
                    ];
                }

                return
                [
                    new PlaceDetails
                    {
                        PlaceId =
                            "istanbul-palace",
                        Name = "Topkapi Palace",
                        Address =
                            "Fatih, Istanbul, Türkiye",
                        Rating = 4.7,
                        Types =
                            ["tourist_attraction"]
                    },
                    new PlaceDetails
                    {
                        PlaceId =
                            "istanbul-hotel",
                        Name = "Istanbul City Hotel",
                        Address =
                            "Şişli, Istanbul, Türkiye",
                        Rating = 4.4,
                        Types = ["lodging"]
                    }
                ];
            });

        var outputs = new Queue<string>(
        [
            """
            {"dayNumber":1,"date":"2026-11-01","title":"يوم arrival في إسطنبول","notes":"بداية هادئة للرحلة.","items":[{"startTime":"09:00","endTime":"11:00","title":"زيارة Topkapi Palace","description":"استكشف تاريخ القصر ومعالمه.","location":"Topkapi Palace","placeReference":"P1","category":"activity","estimatedCost":25,"orderIndex":0},{"startTime":"14:00","endTime":"15:00","title":"الإقامة في Istanbul City Hotel","description":"سجّل الوصول واسترح قبل المساء.","location":"Istanbul City Hotel","placeReference":"P2","category":"accommodation","estimatedCost":null,"orderIndex":1}]}
            """,
            """
            {"dayNumber":1,"date":"2026-11-01","title":"الوصول إلى إسطنبول","notes":"بداية هادئة ومريحة للرحلة.","items":[{"startTime":"09:00","endTime":"11:00","title":"زيارة Topkapi Palace","description":"استكشف تاريخ القصر ومعالمه.","location":"Topkapi Palace","placeReference":"P1","category":"activity","estimatedCost":25,"orderIndex":0},{"startTime":"14:00","endTime":"15:00","title":"تسجيل الإقامة في Istanbul City Hotel","description":"سجّل الوصول واسترح قبل جولة المساء.","location":"Istanbul City Hotel","placeReference":"P2","category":"accommodation","estimatedCost":null,"orderIndex":1}]}
            """
        ]);

        aiProvider
            .Setup(provider =>
                provider.GetChatCompletionAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<ChatMessageDto>>(),
                    It.IsAny<string>(),
                    It.IsAny<AICompletionOptions>(),
                    It.IsAny<CancellationToken>()))
            .Callback<string, List<ChatMessageDto>, string, AICompletionOptions, CancellationToken>(
                (_, _, prompt, _, _) =>
                    sentPrompts.Add(prompt))
            .ReturnsAsync(() => outputs.Dequeue());

        var preferences = CreatePreferences() with
        {
            Interests = ["culture"],
            PreferredLanguage = "ar",
            AdditionalNotes = null
        };

        var handler =
            new GenerateItineraryCommandHandler(
                currentUser.Object,
                tripRepository.Object,
                itineraryRepository.Object,
                aiProvider.Object,
                googlePlacesService.Object,
                logger.Object);

        var result = await handler.Handle(
            new GenerateItineraryCommand(
                trip.Id,
                preferences,
                1000m,
                "USD",
                1),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sentPrompts.Should().HaveCount(2);

        sentPrompts[1]
            .Should().Contain(
                "generic English planning words");

        var accommodation = savedItinerary!
            .Days
            .SelectMany(day => day.Items)
            .Single(item =>
                item.Category ==
                ItineraryItemCategory.Accommodation);

        accommodation.EstimatedCost
            .Should().BeNull();

        savedItinerary.Days
            .Single()
            .Title.Should().Be(
                "الوصول إلى إسطنبول");
    }

    [Fact]
    public async Task Handle_Should_ResolveCountryIntoCities_AndPrioritizeCitiesFromNotes()
    {
        var userId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Turkey Trip",
            Destination = "Turkey",
            OriginCountry = "Egypt",
            SelectedCitiesJson =
                """[{"name":"Istanbul","days":1},{"name":"Cappadocia","days":1}]""",
            StartDate = new DateTime(2026, 12, 1),
            EndDate = new DateTime(2026, 12, 2),
            CreatedAt = DateTime.UtcNow
        };

        var currentUser =
            new Mock<ICurrentUserService>();
        var tripRepository =
            new Mock<ITripRepository>();
        var itineraryRepository =
            new Mock<IItineraryRepository>();
        var aiProvider =
            new Mock<IAIProvider>();
        var googlePlacesService =
            new Mock<IGooglePlacesService>();
        var logger =
            new Mock<
                ILogger<
                    GenerateItineraryCommandHandler>>();

        var sentPrompts = new List<string>();
        Itinerary? savedItinerary = null;
        var travelDataService =
            new Mock<IItineraryTravelDataService>();

        travelDataService
            .SetupGet(service => service.IsConfigured)
            .Returns(true);

        travelDataService
            .Setup(service =>
                service.SearchLocalAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        currentUser
            .Setup(service => service.UserId)
            .Returns(userId);

        tripRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    trip.Id,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(trip);

        itineraryRepository
            .Setup(repository =>
                repository.AddAsync(
                    It.IsAny<Itinerary>(),
                    It.IsAny<CancellationToken>()))
            .Callback<Itinerary, CancellationToken>(
                (itinerary, _) =>
                    savedItinerary = itinerary)
            .Returns(Task.CompletedTask);

        googlePlacesService
            .Setup(service =>
                service.TextSearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<string>()))
            .ReturnsAsync((
                string query,
                CancellationToken _,
                string _) =>
            {
                if (string.Equals(
                        query,
                        "Turkey",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return
                    [
                        new PlaceDetails
                        {
                            PlaceId = "country-turkey",
                            Name = "Turkey",
                            Address = "Turkey",
                            Types = ["country"]
                        }
                    ];
                }

                if (query.StartsWith(
                        "Istanbul",
                        StringComparison.OrdinalIgnoreCase) &&
                    !query.Contains(
                        " in ",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return
                    [
                        new PlaceDetails
                        {
                            PlaceId = "city-istanbul",
                            Name = "Istanbul",
                            Address = "Istanbul, Turkey",
                            Types = ["locality"]
                        }
                    ];
                }

                if (query.StartsWith(
                        "Cappadocia",
                        StringComparison.OrdinalIgnoreCase) &&
                    !query.Contains(
                        " in ",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return
                    [
                        new PlaceDetails
                        {
                            PlaceId = "region-cappadocia",
                            Name = "Cappadocia",
                            Address = "Nevsehir, Turkey",
                            Types = ["administrative_area_level_2"]
                        }
                    ];
                }

                if (query.Contains(
                        "Istanbul",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return
                    [
                        new PlaceDetails
                        {
                            PlaceId = "istanbul-attraction",
                            Name = "Istanbul Landmark",
                            Address = "Fatih, Istanbul, Turkey",
                            Rating = 4.8,
                            Types = ["tourist_attraction"]
                        }
                    ];
                }

                return
                [
                    new PlaceDetails
                    {
                        PlaceId = "cappadocia-attraction",
                        Name = "Cappadocia Valley",
                        Address = "Goreme, Cappadocia, Turkey",
                        Rating = 4.9,
                        Types = ["tourist_attraction"]
                    }
                ];
            });

        var outputs = new Queue<string>(
        [
            """
            {"dayNumber":1,"date":"2026-12-01","title":"Istanbul","notes":null,"items":[{"startTime":"08:00","endTime":"09:00","title":"Breakfast - try menemen","description":"Start with a popular local breakfast.","location":"Istanbul","placeReference":null,"category":"meal","estimatedCost":10,"orderIndex":0},{"startTime":"09:00","endTime":"11:00","title":"Visit Istanbul Landmark","description":"See a celebrated city landmark.","location":"Istanbul Landmark","placeReference":"P1","category":"activity","estimatedCost":20,"orderIndex":1}]}
            """,
            """
            {"dayNumber":2,"date":"2026-12-02","title":"Cappadocia","notes":null,"items":[{"startTime":"08:00","endTime":"09:00","title":"Breakfast - try menemen","description":"Start with a popular local breakfast.","location":"Cappadocia","placeReference":null,"category":"meal","estimatedCost":10,"orderIndex":0},{"startTime":"09:00","endTime":"11:00","title":"Visit Cappadocia Valley","description":"Explore the distinctive valley landscape.","location":"Cappadocia Valley","placeReference":"P2","category":"activity","estimatedCost":25,"orderIndex":1}]}
            """
        ]);

        aiProvider
            .Setup(provider =>
                provider.GetChatCompletionAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<ChatMessageDto>>(),
                    It.IsAny<string>(),
                    It.IsAny<AICompletionOptions>(),
                    It.IsAny<CancellationToken>()))
            .Callback<string, List<ChatMessageDto>, string, AICompletionOptions, CancellationToken>(
                (_, _, prompt, _, _) =>
                    sentPrompts.Add(prompt))
            .ReturnsAsync(() => outputs.Dequeue());

        var preferences = CreatePreferences() with
        {
            Pace = "relaxed",
            AccommodationStyle = "already_booked",
            AdditionalNotes =
                "Visit Istanbul first, then Cappadocia."
        };

        var handler =
            new GenerateItineraryCommandHandler(
                currentUser.Object,
                tripRepository.Object,
                itineraryRepository.Object,
                aiProvider.Object,
                googlePlacesService.Object,
                logger.Object,
                travelDataService.Object);

        var result = await handler.Handle(
            new GenerateItineraryCommand(
                trip.Id,
                preferences,
                1000m,
                "USD",
                1),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sentPrompts.Should().HaveCount(2);
        sentPrompts[0].Should().Contain(
            "Days 1-1: Istanbul");
        sentPrompts[1].Should().Contain(
            "Days 2-2: Cappadocia");
        sentPrompts[0].Should().Contain(
            "city:Istanbul");
        sentPrompts[1].Should().Contain(
            "city:Cappadocia");
        sentPrompts[1].Should().Contain(
            "Previous city: Istanbul");
        sentPrompts[1].Should().Contain(
            "menemen");

        savedItinerary.Should().NotBeNull();
        savedItinerary!.Days
            .OrderBy(day => day.DayNumber)
            .First().Items
            .Single(item =>
                item.Category ==
                ItineraryItemCategory.Activity)
            .Location.Should()
            .Be("Istanbul Landmark");
        savedItinerary.Days
            .OrderBy(day => day.DayNumber)
            .Last().Items
            .Single(item =>
                item.Category ==
                ItineraryItemCategory.Activity)
            .Location.Should()
            .Be("Cappadocia Valley");
        savedItinerary.Days
            .OrderBy(day => day.DayNumber)
            .Last().Items
            .Single(item =>
                item.Category ==
                ItineraryItemCategory.Meal)
            .Title.Should()
            .Contain(
                "different local breakfast specialty");

        travelDataService.Verify(
            service =>
                service.SearchLocalAsync(
                    It.IsAny<string>(),
                    "Istanbul",
                    "en",
                    It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);

        travelDataService.Verify(
            service =>
                service.SearchLocalAsync(
                    It.IsAny<string>(),
                    "Cappadocia",
                    "en",
                    It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_Should_ReturnForbidden_WhenTripBelongsToAnotherUser()
    {
        var currentUser =
            new Mock<ICurrentUserService>();

        var tripRepository =
            new Mock<ITripRepository>();

        var itineraryRepository =
            new Mock<IItineraryRepository>();

        var aiProvider =
            new Mock<IAIProvider>();

        var googlePlacesService =
            new Mock<IGooglePlacesService>();

        var logger =
            new Mock<
                ILogger<
                    GenerateItineraryCommandHandler>>();

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };

        currentUser
            .Setup(service => service.UserId)
            .Returns(Guid.NewGuid());

        tripRepository
            .Setup(repository =>
                repository.GetByIdAsync(
                    trip.Id,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(trip);

        var handler =
            new GenerateItineraryCommandHandler(
                currentUser.Object,
                tripRepository.Object,
                itineraryRepository.Object,
                aiProvider.Object,
                googlePlacesService.Object,
                logger.Object);

        var command =
            new GenerateItineraryCommand(
                trip.Id,
                CreatePreferences(),
                800m,
                "USD",
                1);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();

        result.Error.Code
            .Should().Be("Itinerary.Forbidden");

        aiProvider.VerifyNoOtherCalls();
        googlePlacesService.VerifyNoOtherCalls();
    }

    private static GenerateItineraryPreferences
        CreatePreferences()
    {
        return new GenerateItineraryPreferences
        {
            Interests =
            [
                "culture",
                "food"
            ],
            Pace = "balanced",
            Transportation =
            [
                "walking",
                "public_transport"
            ],
            AccommodationStyle = "mid_range",
            DietaryNeeds = ["halal"],
            AccessibilityNeeds = [],
            PreferredLanguage = "en",
            AdditionalNotes =
                "Include traditional local experiences."
        };
    }
}
