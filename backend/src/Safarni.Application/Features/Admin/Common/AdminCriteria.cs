using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Admin.Common;

public sealed record UserSearchCriteria(
    string? Search = null,
    bool? IsActive = null,
    UserRole? Role = null,
    bool? IsEmailVerified = null,
    DateTime? CreatedFrom = null,
    DateTime? CreatedTo = null,
    string? SortBy = null,
    string? SortDirection = null,
    int Page = 1,
    int PageSize = 20);

public sealed record KnowledgeSearchCriteria(
    string? Search = null,
    string? Category = null,
    bool? IsArchived = false,
    int Page = 1,
    int PageSize = 20);

public sealed record AiActivityCriteria(
    Guid? UserId = null,
    string? Provider = null,
    string? Status = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 20);

public sealed record AuditLogCriteria(
    string? Search = null,
    string? Action = null,
    string? TargetType = null,
    Guid? ActorUserId = null,
    AuditSeverity? Severity = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 20);
