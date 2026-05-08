using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.ProductCatalog.Services;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Craftsman.Infra.Repositories;
using Craftsman.Domain.Inventory.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Integration;

public sealed class PersistenceRepositoryTests
{
    [Fact]
    public async Task Order_repository_saves_and_loads_normalized_order_with_items_and_origin()
    {
        await using var dbContext = CreateDbContext();
        var repository = new OrderRepository(dbContext);
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Shopee", "SO-200"),
            [new OrderItem(Guid.NewGuid(), "SKU-1", "Item", 1, new Money(99, "BRL"))],
            shippingDate: new DateOnly(2027, 5, 10));

        await repository.AddAsync(order);
        await dbContext.SaveChangesAsync();

        var loaded = await repository.GetByOriginAsync(new OrderOrigin("Shopee", "SO-200"));

        Assert.NotNull(loaded);
        Assert.Equal(order.Id, loaded.Id);
        Assert.Equal(new DateOnly(2027, 5, 10), loaded.ShippingDate);
        Assert.Single(loaded.Items);
        Assert.Empty(loaded.DomainEvents);
    }

    [Fact]
    public async Task Product_mapping_service_rejects_conflicting_external_mapping()
    {
        await using var dbContext = CreateDbContext();
        var product = new Product(Guid.NewGuid(), "Produto", billOfMaterials: [new BillOfMaterialsItem(Guid.NewGuid(), 2)]);
        product.SetProductionDurationHours(3);
        product.SetHourlyRate(25.50m);
        var productRepository = new ProductRepository(dbContext);
        var mappingRepository = new ProductMappingRepository(dbContext);
        var mappingService = new ProductMappingService(mappingRepository);

        await productRepository.AddAsync(product);
        await dbContext.SaveChangesAsync();

        var loadedProduct = await productRepository.GetByIdAsync(product.Id);
        Assert.NotNull(loadedProduct);
        Assert.Equal(3, loadedProduct.ProductionDurationHours);
        Assert.Equal(25.50m, loadedProduct.HourlyRate);

        await mappingService.CreateMappingAsync("Elo7", "EXT-1", product.Id);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => mappingService.CreateMappingAsync("Elo7", "EXT-1", product.Id));
    }

    [Fact]
    public async Task Product_repository_saves_and_loads_production_hours_and_hourly_rate()
    {
        await using var dbContext = CreateDbContext();
        var repository = new ProductRepository(dbContext);
        var product = new Product(Guid.NewGuid(), "Produto", productionDurationHours: 12, hourlyRate: 45m);

        await repository.AddAsync(product);
        await dbContext.SaveChangesAsync();

        var loaded = await repository.GetByIdAsync(product.Id);

        Assert.NotNull(loaded);
        Assert.Equal(12, loaded.ProductionDurationHours);
        Assert.Equal(45m, loaded.HourlyRate);
    }

    [Fact]
    public async Task Raw_material_repository_saves_and_loads_low_stock_levels()
    {
        await using var dbContext = CreateDbContext();
        var repository = new RawMaterialRepository(dbContext);
        var rawMaterial = new RawMaterial(Guid.NewGuid(), "Linha", "un", minimumStockLevel: 10, criticalStockLevel: 2);

        await repository.AddAsync(rawMaterial);
        await dbContext.SaveChangesAsync();

        var loaded = await repository.GetByIdAsync(rawMaterial.Id);

        Assert.NotNull(loaded);
        Assert.Equal(10, loaded.MinimumStockLevel);
        Assert.Equal(2, loaded.CriticalStockLevel);
    }

    [Fact]
    public async Task Stock_movement_repository_loads_signed_adjustments_and_ignores_zero_quantity_rows()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterialId = Guid.NewGuid();
        var repository = new StockMovementRepository(dbContext);

        await dbContext.StockMovements.AddRangeAsync(
            new StockMovementEntity
            {
                Id = Guid.NewGuid(),
                RawMaterialId = rawMaterialId,
                Type = StockMovementType.Adjustment.ToString(),
                Quantity = -2,
                Reason = "Inventory count",
                OccurredAt = DateTimeOffset.UtcNow
            },
            new StockMovementEntity
            {
                Id = Guid.NewGuid(),
                RawMaterialId = rawMaterialId,
                Type = StockMovementType.Inbound.ToString(),
                Quantity = 0,
                Reason = "Legacy no-op",
                OccurredAt = DateTimeOffset.UtcNow
            });
        await dbContext.SaveChangesAsync();

        var movements = await repository.GetByRawMaterialAsync(rawMaterialId);

        var movement = Assert.Single(movements);
        Assert.Equal(StockMovementType.Adjustment, movement.Type);
        Assert.Equal(-2, movement.Quantity);
        Assert.Equal(-2, await repository.GetBalanceAsync(rawMaterialId));
    }

    [Fact]
    public async Task Production_task_repository_updates_loaded_task_without_tracking_conflict()
    {
        await using var dbContext = CreateDbContext();
        var repository = new ProductionTaskRepository(dbContext);
        var task = new ProductionTask(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            3,
            plannedStartAt: new DateTimeOffset(2026, 5, 10, 8, 0, 0, TimeSpan.Zero),
            plannedAt: new DateTimeOffset(2026, 5, 10, 11, 0, 0, TimeSpan.Zero));

        await repository.AddAsync(task);
        await dbContext.SaveChangesAsync();

        var loaded = await repository.GetByIdAsync(task.Id);
        Assert.NotNull(loaded);

        loaded.Start();
        await repository.UpdateAsync(loaded);
        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();
        var persisted = await repository.GetByIdAsync(task.Id);

        Assert.NotNull(persisted);
        Assert.Equal(ProductionTaskStatus.InProduction, persisted.Status);
        Assert.Equal(3, persisted.ProductionDurationHours);
        Assert.Equal(new DateTimeOffset(2026, 5, 10, 8, 0, 0, TimeSpan.Zero), persisted.PlannedStartAt);
        Assert.Equal(new DateTimeOffset(2026, 5, 10, 11, 0, 0, TimeSpan.Zero), persisted.PlannedAt);
        Assert.NotNull(persisted.StartedAt);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
