using Craftsman.Infra.Persistence.Entities;
using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Infra.Persistence;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    private readonly ICurrentUserContext? currentUserContext;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserContext currentUserContext)
        : base(options)
    {
        this.currentUserContext = currentUserContext;
    }

    public DbSet<PersistedDomainEventEntity> DomainEvents => Set<PersistedDomainEventEntity>();

    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();

    public DbSet<OrderSourceEntity> OrderSources => Set<OrderSourceEntity>();

    public DbSet<OrderItemEntity> OrderItems => Set<OrderItemEntity>();

    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    public DbSet<BillOfMaterialsItemEntity> BillOfMaterialsItems => Set<BillOfMaterialsItemEntity>();

    public DbSet<ProductMappingEntity> ProductMappings => Set<ProductMappingEntity>();

    public DbSet<RawMaterialEntity> RawMaterials => Set<RawMaterialEntity>();

    public DbSet<StockMovementEntity> StockMovements => Set<StockMovementEntity>();

    public DbSet<ProductionTaskEntity> ProductionTasks => Set<ProductionTaskEntity>();

    public DbSet<ShipmentEntity> Shipments => Set<ShipmentEntity>();

    public DbSet<FinancialSettlementEntity> FinancialSettlements => Set<FinancialSettlementEntity>();

    public DbSet<ShopeeShopEntity> ShopeeShops => Set<ShopeeShopEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditLogs = BuildAuditLogs();
        if (auditLogs.Count > 0)
        {
            await AuditLogs.AddRangeAsync(auditLogs, cancellationToken);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private List<AuditLogEntity> BuildAuditLogs()
    {
        ChangeTracker.DetectChanges();

        var currentUser = currentUserContext?.Current ?? CurrentUserInfo.Anonymous();
        var now = DateTimeOffset.UtcNow;
        var auditLogs = new List<AuditLogEntity>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (!ShouldAudit(entry))
            {
                continue;
            }

            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Modified => AuditAction.Modified,
                EntityState.Deleted => AuditAction.Deleted,
                _ => string.Empty
            };

            var before = entry.State is EntityState.Added ? null : ValuesFor(entry, useOriginalValues: true);
            var after = entry.State is EntityState.Deleted ? null : ValuesFor(entry, useOriginalValues: false);

            auditLogs.Add(new AuditLogEntity
            {
                OccurredAt = now,
                UserId = currentUser.UserId,
                UserName = currentUser.UserName,
                RoleNames = string.Join(",", currentUser.RoleNames),
                Action = action,
                EntityName = entry.Metadata.ClrType.Name,
                EntityId = GetEntityId(entry),
                CorrelationId = currentUser.CorrelationId,
                RequestPath = currentUser.RequestPath,
                BeforeJson = AuditJsonRedactor.SerializeRedacted(before),
                AfterJson = AuditJsonRedactor.SerializeRedacted(after)
            });
        }

        return auditLogs;
    }

    private static bool ShouldAudit(EntityEntry entry)
    {
        if (entry.Entity is AuditLogEntity)
        {
            return false;
        }

        if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            return false;
        }

        return entry.State != EntityState.Modified || entry.Properties.Any(property => property.IsModified);
    }

    private static Dictionary<string, object?> ValuesFor(EntityEntry entry, bool useOriginalValues)
    {
        var values = new Dictionary<string, object?>();
        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsShadowProperty())
            {
                continue;
            }

            values[property.Metadata.Name] = useOriginalValues ? property.OriginalValue : property.CurrentValue;
        }

        return values;
    }

    private static string? GetEntityId(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null)
        {
            return null;
        }

        var values = key.Properties
            .Select(property => entry.Property(property.Name).CurrentValue?.ToString())
            .Where(value => !string.IsNullOrWhiteSpace(value));

        return string.Join(",", values);
    }
}
