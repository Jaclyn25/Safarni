using Safarni.Domain.Enums;

namespace Safarni.Application.Abstractions.Auditing;

public interface IAuditLogWriter
{
    Task WriteAsync(
        Guid? actorUserId,
        string action,
        string targetType,
        string? targetId = null,
        object? metadata = null,
        AuditSeverity severity = AuditSeverity.Info,
        CancellationToken cancellationToken = default);
}
