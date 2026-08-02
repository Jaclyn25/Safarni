using ChatBotApi.Domain.Entities;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Common;
using Safarni.Domain.Entities;

namespace Safarni.Application.Abstractions.Persistence;

public interface IAdminRepository
{
    Task<DashboardSummaryData> GetDashboardSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecentActivityItem>> GetRecentActivityAsync(
        int limit,
        CancellationToken cancellationToken = default);

    Task<PagedResult<User>> SearchUsersAsync(
        UserSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<AdminUserCounts> GetUserCountsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<TravelKnowledge>> SearchKnowledgeAsync(
        KnowledgeSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<TravelKnowledge?> GetKnowledgeByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task AddKnowledgeAsync(
        TravelKnowledge item,
        CancellationToken cancellationToken = default);

    Task SaveKnowledgeChangesAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<AIRequestLog>> SearchAiActivityAsync(
        AiActivityCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<AdminConversationData?> GetConversationForAdminAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task<AnalyticsOverviewData> GetAnalyticsOverviewAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<AnalyticsFunnelData> GetAnalyticsFunnelAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopDestinationData>> GetTopDestinationsAsync(
        int limit,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AuditLog>> SearchAuditLogsAsync(
        AuditLogCriteria criteria,
        CancellationToken cancellationToken = default);
}
