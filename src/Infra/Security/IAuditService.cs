namespace Craftsman.Infra.Security;

public interface IAuditService
{
    Task RecordAsync(
        string action,
        string entityName,
        string? entityId = null,
        object? before = null,
        object? after = null,
        CancellationToken cancellationToken = default);
}
