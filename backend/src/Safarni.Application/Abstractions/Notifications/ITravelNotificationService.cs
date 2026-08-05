namespace Safarni.Application.Abstractions.Notifications;

/// <summary>
/// Sends travel-related notification emails (for example the itinerary
/// summary after a successful generation) while respecting the user's
/// notification settings. Implementations must never throw.
/// </summary>
public interface ITravelNotificationService
{
    Task SendItinerarySummaryAsync(
        Guid tripId,
        Guid userId,
        Guid itineraryId,
        CancellationToken cancellationToken = default);
}
