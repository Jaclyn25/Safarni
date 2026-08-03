using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Application.Features.Notifications;
using Safarni.Domain.Entities;

namespace Safarni.Application.UnitTests.Notifications;

public class TravelNotificationServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _tripId = Guid.NewGuid();
    private readonly Guid _itineraryId = Guid.NewGuid();

    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUserSettingsRepository> _settingsRepository = new();
    private readonly Mock<ITripRepository> _tripRepository = new();
    private readonly Mock<IItineraryRepository> _itineraryRepository = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IItineraryLinkFactory> _itineraryLinkFactory = new();

    private TravelNotificationService CreateService()
    {
        return new TravelNotificationService(
            _userRepository.Object,
            _settingsRepository.Object,
            _tripRepository.Object,
            _itineraryRepository.Object,
            _emailService.Object,
            _itineraryLinkFactory.Object,
            NullLogger<TravelNotificationService>.Instance);
    }

    private void SetupHappyPath()
    {
        var user = new User { Id = _userId, FullName = "Sara Traveler", Email = "sara@example.com" };
        var settings = new UserSettings { UserId = _userId, EmailNotificationsEnabled = true };
        var trip = new Trip
        {
            Id = _tripId,
            UserId = _userId,
            Name = "Cairo Getaway",
            Destination = "Cairo",
            StartDate = new DateTime(2026, 8, 3),
            EndDate = new DateTime(2026, 8, 8)
        };
        var itinerary = new Itinerary
        {
            Id = _itineraryId,
            TripId = _tripId
        };
        var dayId = Guid.NewGuid();
        var day = new ItineraryDay
        {
            Id = dayId,
            DayNumber = 1,
            Items =
            {
                new ItineraryItem
                {
                    ItineraryDayId = dayId,
                    Title = "Escape the city"
                }
            }
        };
        itinerary.Days.Add(day);

        _userRepository
            .Setup(repository => repository.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _settingsRepository
            .Setup(repository => repository.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);
        _tripRepository
            .Setup(repository => repository.GetByIdAsync(_tripId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(trip);
        _itineraryRepository
            .Setup(repository => repository.GetByIdAsync(_itineraryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(itinerary);
        _itineraryLinkFactory
            .Setup(factory => factory.Create(_tripId))
            .Returns($"http://localhost:4200/planner/{_tripId}");
        _emailService
            .Setup(service => service.SendItinerarySummaryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task SendItinerarySummaryAsync_Should_SendEmail_WhenEmailNotificationsEnabled()
    {
        SetupHappyPath();
        var service = CreateService();

        await service.SendItinerarySummaryAsync(
            _tripId,
            _userId,
            _itineraryId,
            CancellationToken.None);

        _emailService
            .Verify(service => service.SendItinerarySummaryAsync(
                "sara@example.com",
                "Sara Traveler",
                "Cairo Getaway",
                "Cairo",
                new DateTime(2026, 8, 3),
                new DateTime(2026, 8, 8),
                It.IsAny<string>(),
                $"http://localhost:4200/planner/{_tripId}",
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendItinerarySummaryAsync_Should_SendEmail_WhenSettingsRowMissing()
    {
        SetupHappyPath();
        _settingsRepository
            .Setup(repository => repository.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSettings?)null);
        var service = CreateService();

        await service.SendItinerarySummaryAsync(
            _tripId,
            _userId,
            _itineraryId,
            CancellationToken.None);

        _emailService
            .Verify(service => service.SendItinerarySummaryAsync(
                "sara@example.com",
                "Sara Traveler",
                "Cairo Getaway",
                "Cairo",
                new DateTime(2026, 8, 3),
                new DateTime(2026, 8, 8),
                It.IsAny<string>(),
                $"http://localhost:4200/planner/{_tripId}",
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendItinerarySummaryAsync_Should_NotSend_WhenEmailNotificationsDisabled()
    {
        SetupHappyPath();
        _settingsRepository
            .Setup(repository => repository.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSettings
            {
                UserId = _userId,
                EmailNotificationsEnabled = false
            });
        var service = CreateService();

        await service.SendItinerarySummaryAsync(
            _tripId,
            _userId,
            _itineraryId,
            CancellationToken.None);

        _emailService
            .Verify(service => service.SendItinerarySummaryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendItinerarySummaryAsync_Should_NotSend_WhenUserHasNoEmail()
    {
        SetupHappyPath();
        _userRepository
            .Setup(repository => repository.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = _userId, FullName = "No Email", Email = string.Empty });
        var service = CreateService();

        await service.SendItinerarySummaryAsync(
            _tripId,
            _userId,
            _itineraryId,
            CancellationToken.None);

        _emailService
            .Verify(service => service.SendItinerarySummaryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendItinerarySummaryAsync_Should_IncludeHighlightsAndLink_WhenEmailSent()
    {
        SetupHappyPath();
        var service = CreateService();

        await service.SendItinerarySummaryAsync(
            _tripId,
            _userId,
            _itineraryId,
            CancellationToken.None);

        _emailService
            .Verify(service => service.SendItinerarySummaryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.Is<string>(summary => summary.Contains("Day 1") && summary.Contains("Escape the city")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendItinerarySummaryAsync_Should_NotThrow_WhenEmailServiceFails()
    {
        SetupHappyPath();
        _emailService
            .Setup(service => service.SendItinerarySummaryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP is down"));
        var service = CreateService();

        var act = () => service.SendItinerarySummaryAsync(
            _tripId,
            _userId,
            _itineraryId,
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendItinerarySummaryAsync_Should_NotThrow_WhenRepositoriesThrow()
    {
        _userRepository
            .Setup(repository => repository.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database is down"));
        var service = CreateService();

        var act = () => service.SendItinerarySummaryAsync(
            _tripId,
            _userId,
            _itineraryId,
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}