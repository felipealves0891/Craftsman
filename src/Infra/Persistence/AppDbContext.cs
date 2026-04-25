using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Infra.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<PersistedDomainEventEntity> DomainEvents => Set<PersistedDomainEventEntity>();

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();

    public DbSet<OrderItemEntity> OrderItems => Set<OrderItemEntity>();

    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    public DbSet<BillOfMaterialsItemEntity> BillOfMaterialsItems => Set<BillOfMaterialsItemEntity>();

    public DbSet<ProductMappingEntity> ProductMappings => Set<ProductMappingEntity>();

    public DbSet<RawMaterialEntity> RawMaterials => Set<RawMaterialEntity>();

    public DbSet<StockMovementEntity> StockMovements => Set<StockMovementEntity>();

    public DbSet<ProductionTaskEntity> ProductionTasks => Set<ProductionTaskEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
