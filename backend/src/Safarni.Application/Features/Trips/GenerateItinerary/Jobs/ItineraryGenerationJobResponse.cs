using Safarni.Domain.Entities;

namespace Safarni.Application.Features.Trips.GenerateItinerary.Jobs;

public sealed record ItineraryGenerationJobResponse(
    Guid Id,
    Guid TripId,
    string Status,
    int ProgressPercent,
    string Stage,
    string? ErrorCode,
    string? ErrorMessage,
    Guid? ItineraryId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt);

internal static class ItineraryGenerationJobResponseMapper
{
    public static ItineraryGenerationJobResponse Map(
        ItineraryGenerationJob job)
    {
        return new ItineraryGenerationJobResponse(
            job.Id,
            job.TripId,
            job.Status.ToString().ToLowerInvariant(),
            job.ProgressPercent,
            job.Stage,
            job.ErrorCode,
            job.ErrorMessage,
            job.ItineraryId,
            job.CreatedAt,
            job.UpdatedAt,
            job.StartedAt,
            job.CompletedAt);
    }
}
