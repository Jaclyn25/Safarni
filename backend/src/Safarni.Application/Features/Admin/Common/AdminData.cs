namespace Safarni.Application.Features.Admin.Common;

public sealed record DashboardSummaryData(
    int TotalUsers,
    int ActiveUsers,
    int VerifiedUsers,
    int TotalTrips,
    int TotalConversations,
    int TotalAiMessages,
    int ItineraryJobsTotal,
    int ItineraryJobsPending,
    int ItineraryJobsFailed,
    int RecentAiFailuresLast24Hours,
    bool QdrantHealthy,
    int DegradedServiceCount);

public sealed record RecentActivityItem(
    string Type,
    string Title,
    string? Detail,
    Guid? ActorUserId,
    string? ActorName,
    DateTime Timestamp);

public sealed record AdminUserCounts(
    int Trips,
    int Conversations,
    int Messages);

public sealed record AdminConversationMessageData(
    Guid Id,
    string Role,
    string Content,
    DateTime CreatedAt);

public sealed record AdminConversationData(
    Guid Id,
    string Title,
    Guid UserId,
    string UserEmail,
    string UserFullName,
    DateTime CreatedAt,
    bool IsPinned,
    IReadOnlyList<AdminConversationMessageData> Messages);

public sealed record AnalyticsDailyPoint(
    DateTime Date,
    int Users,
    int Trips,
    int Messages,
    int ItineraryJobs);

public sealed record AnalyticsOverviewData(
    IReadOnlyList<AnalyticsDailyPoint> Points);

public sealed record AnalyticsFunnelData(
    int RegisteredUsers,
    int VerifiedUsers,
    int UsersWithTrips,
    int UsersWithGeneratedItineraries);

public sealed record TopDestinationData(
    string Name,
    int TripCount);
