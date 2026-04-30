namespace Craftsman.Infra.Persistence.Entities;

public sealed class AuditLogEntity : IPersistenceEntity<long>
{
    public long Id { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public int? UserId { get; set; }

    public string? UserName { get; set; }

    public string RoleNames { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? CorrelationId { get; set; }

    public string? RequestPath { get; set; }

    public string? BeforeJson { get; set; }

    public string? AfterJson { get; set; }
}
