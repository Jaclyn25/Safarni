-- Trip publishability snapshot
-- Run this in your Safarni SQL database to inspect why "Make Public" is disabled.

-- 1) Snapshot: trip status + active itinerary + latest generation job
SELECT
    t.Id AS TripId,
    t.[Name] AS TripName,
    t.[Status] AS TripStatus,
    t.IsPublished,
    t.Visibility,
    t.PublishedAt,
    ai.Id AS ActiveItineraryId,
    ai.[Status] AS ActiveItineraryStatus,
    ai.UpdatedAt AS ActiveItineraryUpdatedAt,
    j.Id AS LatestJobId,
    j.[Status] AS LatestJobStatus,
    j.ProgressPercent AS LatestJobProgress,
    j.CompletedAt AS LatestJobCompletedAt,
    t.UpdatedAt AS TripUpdatedAt,
    t.CreatedAt AS TripCreatedAt
FROM Trips t
OUTER APPLY (
    SELECT TOP (1)
        i.Id,
        i.[Status],
        i.UpdatedAt
    FROM Itineraries i
    WHERE i.TripId = t.Id
      AND i.IsActive = 1
    ORDER BY i.UpdatedAt DESC
) ai
OUTER APPLY (
    SELECT TOP (1)
        g.Id,
        g.[Status],
        g.ProgressPercent,
        g.CompletedAt,
        g.UpdatedAt
    FROM ItineraryGenerationJobs g
    WHERE g.TripId = t.Id
    ORDER BY g.UpdatedAt DESC
) j
ORDER BY COALESCE(t.UpdatedAt, t.CreatedAt) DESC;

-- 2) Focused check: rows that cannot be published by backend rules
-- Backend currently requires: Trip.Status = 'Completed' and an active itinerary with days.
SELECT
    t.Id AS TripId,
    t.[Name] AS TripName,
    t.[Status] AS TripStatus,
    ai.Id AS ActiveItineraryId,
    ai.[Status] AS ActiveItineraryStatus
FROM Trips t
LEFT JOIN Itineraries ai
    ON ai.TripId = t.Id
   AND ai.IsActive = 1
WHERE t.[Status] <> 'Completed'
   OR ai.Id IS NULL
ORDER BY t.CreatedAt DESC;

-- 3) Optional one-time data repair for legacy trips (review before running)
-- This marks trips as Completed when they already have an active completed itinerary.
-- Uncomment to execute:
/*
UPDATE t
SET
    t.[Status] = 'Completed',
    t.UpdatedAt = SYSUTCDATETIME()
FROM Trips t
INNER JOIN Itineraries i
    ON i.TripId = t.Id
   AND i.IsActive = 1
   AND i.[Status] = 'Completed'
WHERE t.[Status] <> 'Completed';
*/
