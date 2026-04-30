using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;

namespace Craftsman.Infra.Security;

public sealed class AuditService : IAuditService
{
    private readonly AppDbContext dbContext;
    private readonly ICurrentUserContext currentUserContext;

    public AuditService(AppDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        this.dbContext = dbContext;
        this.currentUserContext = currentUserContext;
    }

    public async Task RecordAsync(
        string action,
        string entityName,
        string? entityId = null,
        object? before = null,
        object? after = null,
        CancellationToken cancellationToken = default)
    {
        var currentUser = currentUserContext.Current;
        await dbContext.AuditLogs.AddAsync(
            new AuditLogEntity
            {
                OccurredAt = DateTimeOffset.UtcNow,
                UserId = currentUser.UserId,
                UserName = currentUser.UserName,
                RoleNames = string.Join(",", currentUser.RoleNames),
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                CorrelationId = currentUser.CorrelationId,
                RequestPath = currentUser.RequestPath,
                BeforeJson = AuditJsonRedactor.SerializeRedacted(before),
                AfterJson = AuditJsonRedactor.SerializeRedacted(after)
            },
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
