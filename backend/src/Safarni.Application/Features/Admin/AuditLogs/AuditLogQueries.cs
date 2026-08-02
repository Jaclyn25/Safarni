using MediatR;
using Safarni.Application.Abstractions.Persistence;
using Safarni.Application.Common.Results;
using Safarni.Application.Features.Admin.Common;
using Safarni.Domain.Entities;
using Safarni.Domain.Enums;

namespace Safarni.Application.Features.Admin.AuditLogs;

public sealed record AuditLogResponse(
    Guid Id,
    Guid? ActorUserId,
    string Action,
    string TargetType,
    string? TargetId,
    string MetadataJson,
    string? IpAddress,
    string? UserAgent,
    AuditSeverity Severity,
    DateTime CreatedAt);

public sealed record ListAuditLogsQuery(AuditLogCriteria Criteria)
    : IRequest<Result<PagedResult<AuditLogResponse>>>;

public static class AuditLogResponseMapper
{
    public static AuditLogResponse ToResponse(AuditLog log)
    {
        return new AuditLogResponse(
            log.Id,
            log.ActorUserId,
            log.Action,
            log.TargetType,
            log.TargetId,
            log.MetadataJson,
            log.IpAddress,
            log.UserAgent,
            log.Severity,
            log.CreatedAt);
    }
}

public sealed class ListAuditLogsQueryHandler
    : IRequestHandler<ListAuditLogsQuery, Result<PagedResult<AuditLogResponse>>>
{
    private readonly IAdminRepository _adminRepository;

    public ListAuditLogsQueryHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task<Result<PagedResult<AuditLogResponse>>> Handle(
        ListAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _adminRepository.SearchAuditLogsAsync(
            request.Criteria,
            cancellationToken);

        return Result<PagedResult<AuditLogResponse>>.Success(
            new PagedResult<AuditLogResponse>(
                result.Items
                    .Select(AuditLogResponseMapper.ToResponse)
                    .ToArray(),
                result.Page,
                result.PageSize,
                result.TotalCount));
    }
}
