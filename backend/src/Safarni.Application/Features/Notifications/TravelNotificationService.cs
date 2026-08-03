using System.Net;
using Microsoft.Extensions.Logging;
using Safarni.Application.Abstractions.Notifications;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Abstractions.Security;
using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Notifications;

public sealed class TravelNotificationService : ITravelNotificationService
{
    private const int MaxHighlightItems = 6;

    private readonly IUserRepository _userRepository;
    private readonly IUserSettingsRepository _settingsRepository;
    private readonly ITripRepository _tripRepository;
    private readonly IItineraryRepository _itineraryRepository;
    private readonly IEmailService _emailService;
    private readonly IItineraryLinkFactory _itineraryLinkFactory;
    private readonly ILogger<TravelNotificationService> _logger;

    public TravelNotificationService(
        IUserRepository userRepository,
        IUserSettingsRepository settingsRepository,
        ITripRepository tripRepository,
        IItineraryRepository itineraryRepository,
        IEmailService emailService,
        IItineraryLinkFactory itineraryLinkFactory,
        ILogger<TravelNotificationService> logger)
    {
        _userRepository = userRepository;
        _settingsRepository = settingsRepository;
        _tripRepository = tripRepository;
        _itineraryRepository = itineraryRepository;
        _emailService = emailService;
        _itineraryLinkFactory = itineraryLinkFactory;
        _logger = logger;
    }

    public async Task SendItinerarySummaryAsync(
        Guid tripId,
        Guid userId,
        Guid itineraryId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null || string.IsNullOrWhiteSpace(user.Email))
            {
                _logger.LogWarning(
                    "Skipping itinerary summary email for trip {TripId}: user {UserId} not found or has no email.",
                    tripId,
                    userId);
                return;
            }

            var settings = await _settingsRepository.GetByUserIdAsync(userId, cancellationToken);
            var emailNotificationsEnabled = settings?.EmailNotificationsEnabled ?? true;
            if (!emailNotificationsEnabled)
            {
                _logger.LogInformation(
                    "Skipping itinerary summary email for trip {TripId}: email notifications are disabled for user {UserId}.",
                    tripId,
                    userId);
                return;
            }

            var trip = await _tripRepository.GetByIdAsync(tripId, cancellationToken);
            if (trip is null)
            {
                _logger.LogWarning(
                    "Skipping itinerary summary email for user {UserId}: trip {TripId} not found.",
                    userId,
                    tripId);
                return;
            }

            var itinerary = await _itineraryRepository.GetByIdAsync(itineraryId, cancellationToken);
            if (itinerary is null)
            {
                _logger.LogWarning(
                    "Skipping itinerary summary email for user {UserId}: itinerary {ItineraryId} not found.",
                    userId,
                    itineraryId);
                return;
            }

            var summaryHtml = BuildHighlightsHtml(itinerary);
            var itineraryLink = _itineraryLinkFactory.Create(tripId);
            var displayName = string.IsNullOrWhiteSpace(user.FullName)
                ? "traveler"
                : user.FullName;

            await _emailService.SendItinerarySummaryAsync(
                user.Email,
                displayName,
                trip.Name,
                trip.Destination,
                trip.StartDate,
                trip.EndDate,
                summaryHtml,
                itineraryLink,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to send itinerary summary email for trip {TripId} user {UserId}. " +
                "Itinerary generation is unaffected.",
                tripId,
                userId);
        }
    }

    private static string BuildHighlightsHtml(Itinerary itinerary)
    {
        var dayNumbers = itinerary.Days
            .ToDictionary(day => day.Id, day => day.DayNumber);

        var items = itinerary.Days
            .SelectMany(day => day.Items)
            .Where(item => !string.IsNullOrWhiteSpace(item.Title))
            .OrderBy(item => dayNumbers.GetValueOrDefault(item.ItineraryDayId))
            .ThenBy(item => item.OrderIndex)
            .Take(MaxHighlightItems)
            .ToList();

        if (items.Count == 0)
        {
            return string.Empty;
        }

        var listItems = items
            .Select(item =>
            {
                var dayLabel = dayNumbers.TryGetValue(item.ItineraryDayId, out var dayNumber)
                    ? $"Day {dayNumber}: "
                    : string.Empty;

                return $"<li style=\"margin:6px 0;\">{dayLabel}{WebUtility.HtmlEncode(item.Title)}</li>";
            });

        return $"<ul style=\"margin:0 0 0 18px;padding:0;\">{string.Concat(listItems)}</ul>";
    }
}
