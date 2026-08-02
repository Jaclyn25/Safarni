using Safarni.Domain.Enums;

namespace Safarni.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }

    public Guid? ActorUserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public string? TargetId { get; set; }

    public string MetadataJson { get; set; } = "{}";

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public AuditSeverity Severity { get; set; } = AuditSeverity.Info;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
