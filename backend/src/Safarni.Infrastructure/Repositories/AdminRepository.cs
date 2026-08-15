using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Common;
using Safarni.Domain.Entities;
using Safarni.Infrastructure.Persistence;

namespace Safarni.Infrastructure.Repositories;

public sealed class AdminRepository : IAdminRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IQdrantService _qdrantService;

    public AdminRepository(
        ApplicationDbContext dbContext,
        IQdrantService qdrantService)
    {
        _dbContext = dbContext;
        _qdrantService = qdrantService;
    }

    public async Task<DashboardSummaryData> GetDashboardSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var aiFailureThreshold = now.AddHours(-24);

        var totalUsers = await _dbContext.Users.CountAsync(cancellationToken);
        var activeUsers = await _dbContext.Users
            .CountAsync(u => u.IsActive, cancellationToken);
        var verifiedUsers = await _dbContext.Users
            .CountAsync(u => u.IsEmailVerified, cancellationToken);
        var totalTrips = await _dbContext.Trips.CountAsync(cancellationToken);
        var totalConversations = await _dbContext.Conversations
            .CountAsync(cancellationToken);
        var totalAiMessages = await _dbContext.ConversationMessages
            .CountAsync(m => m.Role == "assistant", cancellationToken);
        var itineraryJobsTotal = await _dbContext.ItineraryGenerationJobs
            .CountAsync(cancellationToken);
        var itineraryJobsPending = await _dbContext.ItineraryGenerationJobs
            .CountAsync(
                j => j.Status == ItineraryGenerationJobStatus.Pending ||
                    j.Status == ItineraryGenerationJobStatus.Running,
                cancellationToken);
        var itineraryJobsFailed = await _dbContext.ItineraryGenerationJobs
            .CountAsync(
                j => j.Status == ItineraryGenerationJobStatus.Failed,
                cancellationToken);
        var recentAiFailures = await _dbContext.AIRequestLogs
            .CountAsync(
                log => log.Status != "Success" &&
                    log.CreatedAt >= aiFailureThreshold,
                cancellationToken);

        var qdrantHealthy = false;
        try
        {
            qdrantHealthy = await _qdrantService.IsHealthyAsync();
        }
        catch
        {
            qdrantHealthy = false;
        }

        var degradedServiceCount = 0;
        if (recentAiFailures > 0)
        {
            degradedServiceCount++;
        }

        if (itineraryJobsFailed > 0)
        {
            degradedServiceCount++;
        }

        if (!qdrantHealthy)
        {
            degradedServiceCount++;
        }

        return new DashboardSummaryData(
            TotalUsers: totalUsers,
            ActiveUsers: activeUsers,
            VerifiedUsers: verifiedUsers,
            TotalTrips: totalTrips,
            TotalConversations: totalConversations,
            TotalAiMessages: totalAiMessages,
            ItineraryJobsTotal: itineraryJobsTotal,
            ItineraryJobsPending: itineraryJobsPending,
            ItineraryJobsFailed: itineraryJobsFailed,
            RecentAiFailuresLast24Hours: recentAiFailures,
            QdrantHealthy: qdrantHealthy,
            DegradedServiceCount: degradedServiceCount);
    }

    public async Task<IReadOnlyList<RecentActivityItem>> GetRecentActivityAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        const int perSource = 5;

        var items = new List<RecentActivityItem>();

        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Take(perSource)
            .Select(u => new RecentActivityItem(
                "User.Registered",
                u.FullName,
                u.Email,
                u.Id,
                u.FullName,
                u.CreatedAt))
            .ToListAsync(cancellationToken);
        items.AddRange(users);

        var trips = await _dbContext.Trips
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Take(perSource)
            .Select(t => new RecentActivityItem(
                "Trip.Created",
                t.Name,
                t.Destination,
                t.UserId,
                null,
                t.CreatedAt))
            .ToListAsync(cancellationToken);
        items.AddRange(trips);

        var conversationRows = await _dbContext.Conversations
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Take(perSource)
            .Select(c => new
            {
                c.Title,
                c.UserId,
                c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        items.AddRange(conversationRows.Select(row =>
            new RecentActivityItem(
                "Chat.Created",
                row.Title,
                string.Empty,
                row.UserId,
                null,
                row.CreatedAt)));

        var adminActions = await _dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(perSource)
            .Select(a => new RecentActivityItem(
                "Admin.Action",
                a.Action,
                a.TargetType,
                a.ActorUserId,
                null,
                a.CreatedAt))
            .ToListAsync(cancellationToken);
        items.AddRange(adminActions);

        return items
            .OrderByDescending(item => item.Timestamp)
            .Take(limit)
            .ToArray();
    }

    public async Task<PagedResult<User>> SearchUsersAsync(
        UserSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var search = criteria.Search.Trim();
            query = query.Where(u =>
                u.Email.Contains(search) ||
                u.FullName.Contains(search));
        }

        if (criteria.IsActive is not null)
        {
            query = query.Where(u => u.IsActive == criteria.IsActive);
        }

        if (criteria.Role is not null)
        {
            query = query.Where(u => u.Role == criteria.Role);
        }

        if (criteria.IsEmailVerified is not null)
        {
            query = query.Where(u => u.IsEmailVerified == criteria.IsEmailVerified);
        }

        if (criteria.CreatedFrom is not null)
        {
            query = query.Where(u => u.CreatedAt >= criteria.CreatedFrom.Value);
        }

        if (criteria.CreatedTo is not null)
        {
            query = query.Where(u => u.CreatedAt <= criteria.CreatedTo.Value);
        }

        var descending = string.Equals(
            criteria.SortDirection,
            "desc",
            StringComparison.OrdinalIgnoreCase);

        query = criteria.SortBy?.ToLowerInvariant() switch
        {
            "name" => descending
                ? query.OrderByDescending(u => u.FullName)
                : query.OrderBy(u => u.FullName),
            "email" => descending
                ? query.OrderByDescending(u => u.Email)
                : query.OrderBy(u => u.Email),
            _ => descending
                ? query.OrderByDescending(u => u.CreatedAt)
                : query.OrderBy(u => u.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(criteria.Page, 1);
        var pageSize = Math.Clamp(criteria.PageSize, 1, 100);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<User>(items, page, pageSize, totalCount);
    }

    public async Task<AdminUserCounts> GetUserCountsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var trips = await _dbContext.Trips
            .CountAsync(t => t.UserId == userId, cancellationToken);
        var conversations = await _dbContext.Conversations
            .CountAsync(c => c.UserId == userId, cancellationToken);
        var messages = await _dbContext.ConversationMessages
            .CountAsync(
                m => _dbContext.Conversations.Any(c =>
                    c.Id == m.ConversationId &&
                    c.UserId == userId),
                cancellationToken);

        return new AdminUserCounts(trips, conversations, messages);
    }

    public async Task<PagedResult<TravelKnowledge>> SearchKnowledgeAsync(
        KnowledgeSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TravelKnowledges.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var search = criteria.Search.Trim();
            query = query.Where(k =>
                k.Title.Contains(search) ||
                k.Content.Contains(search) ||
                k.Category.Contains(search) ||
                (k.City != null && k.City.Contains(search)) ||
                (k.Country != null && k.Country.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Category))
        {
            query = query.Where(k => k.Category == criteria.Category);
        }

        if (criteria.IsArchived is not null)
        {
            query = query.Where(k => k.IsArchived == criteria.IsArchived);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(criteria.Page, 1);
        var pageSize = Math.Clamp(criteria.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(k => k.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TravelKnowledge>(items, page, pageSize, totalCount);
    }

    public Task<TravelKnowledge?> GetKnowledgeByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TravelKnowledges
            .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);
    }

    public async Task AddKnowledgeAsync(
        TravelKnowledge item,
        CancellationToken cancellationToken = default)
    {
        _dbContext.TravelKnowledges.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveKnowledgeChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AIRequestLog>> SearchAiActivityAsync(
        AiActivityCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AIRequestLogs.AsNoTracking().AsQueryable();

        if (criteria.UserId is not null)
        {
            query = query.Where(log => log.UserId == criteria.UserId);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Provider))
        {
            query = query.Where(log => log.Provider == criteria.Provider);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            query = query.Where(log => log.Status == criteria.Status);
        }

        if (criteria.From is not null)
        {
            query = query.Where(log => log.CreatedAt >= criteria.From.Value);
        }

        if (criteria.To is not null)
        {
            query = query.Where(log => log.CreatedAt <= criteria.To.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(criteria.Page, 1);
        var pageSize = Math.Clamp(criteria.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(log => log.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AIRequestLog>(items, page, pageSize, totalCount);
    }

    public async Task<AdminConversationData?> GetConversationForAdminAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.Conversations
            .AsNoTracking()
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation is null)
        {
            return null;
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Id == conversation.UserId,
                cancellationToken);

        return new AdminConversationData(
            conversation.Id,
            conversation.Title,
            user?.Id ?? Guid.Empty,
            user?.Email ?? "Unknown user",
            user?.FullName ?? "Unknown user",
            conversation.CreatedAt,
            conversation.IsPinned,
            conversation.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new AdminConversationMessageData(
                    m.Id,
                    m.Role,
                    m.Content,
                    m.CreatedAt))
                .ToArray());
    }

    public async Task<AnalyticsOverviewData> GetAnalyticsOverviewAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var start = from.Date;
        var end = to.Date;

        var usersByDay = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.CreatedAt.Date >= start && u.CreatedAt.Date <= end)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var tripsByDay = await _dbContext.Trips
            .AsNoTracking()
            .Where(t => t.CreatedAt.Date >= start && t.CreatedAt.Date <= end)
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var messagesByDay = await _dbContext.ConversationMessages
            .AsNoTracking()
            .Where(m => m.CreatedAt.Date >= start && m.CreatedAt.Date <= end)
            .GroupBy(m => m.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var jobsByDay = await _dbContext.ItineraryGenerationJobs
            .AsNoTracking()
            .Where(j => j.CreatedAt.Date >= start && j.CreatedAt.Date <= end)
            .GroupBy(j => j.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var points = new List<AnalyticsDailyPoint>();
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            points.Add(new AnalyticsDailyPoint(
                day,
                usersByDay.FirstOrDefault(p => p.Date == day)?.Count ?? 0,
                tripsByDay.FirstOrDefault(p => p.Date == day)?.Count ?? 0,
                messagesByDay.FirstOrDefault(p => p.Date == day)?.Count ?? 0,
                jobsByDay.FirstOrDefault(p => p.Date == day)?.Count ?? 0));
        }

        return new AnalyticsOverviewData(points);
    }

    public async Task<AnalyticsFunnelData> GetAnalyticsFunnelAsync(
        CancellationToken cancellationToken = default)
    {
        var registered = await _dbContext.Users.CountAsync(cancellationToken);
        var verified = await _dbContext.Users
            .CountAsync(u => u.IsEmailVerified, cancellationToken);
        var usersWithTrips = await _dbContext.Trips
            .Select(t => t.UserId)
            .Distinct()
            .CountAsync(cancellationToken);
        var usersWithItineraries = await _dbContext.ItineraryGenerationJobs
            .Where(j => j.Status == ItineraryGenerationJobStatus.Completed)
            .Select(j => j.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        return new AnalyticsFunnelData(
            registered,
            verified,
            usersWithTrips,
            usersWithItineraries);
    }

    public async Task<IReadOnlyList<TopDestinationData>> GetTopDestinationsAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var tripsByDestination = await _dbContext.Trips
            .AsNoTracking()
            .Where(t => !string.IsNullOrWhiteSpace(t.Destination))
            .GroupBy(t => t.Destination.Trim())
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var citiesByCity = await _dbContext.TripCities
            .AsNoTracking()
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .GroupBy(c => c.Name.Trim())
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var merged = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var destination in tripsByDestination)
        {
            merged[destination.Name] = destination.Count;
        }

        foreach (var city in citiesByCity)
        {
            merged[city.Name] = merged.GetValueOrDefault(city.Name) + city.Count;
        }

        return merged
            .OrderByDescending(pair => pair.Value)
            .Take(Math.Clamp(limit, 1, 50))
            .Select(pair => new TopDestinationData(pair.Key, pair.Value))
            .ToArray();
    }

    public async Task<PagedResult<AuditLog>> SearchAuditLogsAsync(
        AuditLogCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var search = criteria.Search.Trim();
            query = query.Where(a =>
                a.Action.Contains(search) ||
                a.TargetType.Contains(search) ||
                (a.TargetId != null && a.TargetId.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Action))
        {
            query = query.Where(a => a.Action == criteria.Action);
        }

        if (!string.IsNullOrWhiteSpace(criteria.TargetType))
        {
            query = query.Where(a => a.TargetType == criteria.TargetType);
        }

        if (criteria.ActorUserId is not null)
        {
            query = query.Where(a => a.ActorUserId == criteria.ActorUserId);
        }

        if (criteria.Severity is not null)
        {
            query = query.Where(a => a.Severity == criteria.Severity);
        }

        if (criteria.From is not null)
        {
            query = query.Where(a => a.CreatedAt >= criteria.From.Value);
        }

        if (criteria.To is not null)
        {
            query = query.Where(a => a.CreatedAt <= criteria.To.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(criteria.Page, 1);
        var pageSize = Math.Clamp(criteria.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLog>(items, page, pageSize, totalCount);
    }
}
