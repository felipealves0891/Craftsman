using Craftsman.App.Services;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Domain.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Inventory;

public sealed class StockAlertAppServiceTests
{
    [Fact]
    public async Task Global_and_critical_alerts_exclude_inactive_materials_and_keep_active_classification()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterials = new RawMaterialRepository(dbContext);
        var stockMovements = new StockMovementRepository(dbContext);
        var activeWarning = new RawMaterial(
            Guid.NewGuid(),
            "Linha",
            "un",
            minimumStockLevel: 10,
            criticalStockLevel: 3);
        var activeCritical = new RawMaterial(
            Guid.NewGuid(),
            "Couro",
            "m2",
            minimumStockLevel: 10,
            criticalStockLevel: 2);
        var inactiveCritical = new RawMaterial(
            Guid.NewGuid(),
            "Tecido desativado",
            "m",
            RawMaterialStatus.Inactive,
            minimumStockLevel: 10,
            criticalStockLevel: 2);

        await rawMaterials.AddAsync(activeWarning);
        await rawMaterials.AddAsync(activeCritical);
        await rawMaterials.AddAsync(inactiveCritical);
        await stockMovements.AddAsync(new StockMovement(
            Guid.NewGuid(), activeWarning.Id, StockMovementType.Inbound, 5, "Compra", null));
        await stockMovements.AddAsync(new StockMovement(
            Guid.NewGuid(), activeCritical.Id, StockMovementType.Inbound, 2, "Compra", null));
        await stockMovements.AddAsync(new StockMovement(
            Guid.NewGuid(), inactiveCritical.Id, StockMovementType.Inbound, 1, "Compra", null));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, rawMaterials, stockMovements);

        var alerts = await service.ListAlertsAsync();
        var criticalAlerts = await service.ListCriticalAlertsAsync();
        var repeatedAlerts = await service.ListAlertsAsync();

        Assert.Collection(
            alerts,
            alert =>
            {
                Assert.Equal(activeCritical.Id, alert.RawMaterialId);
                Assert.Equal(StockAlertLevel.Critical, alert.Level);
            },
            alert =>
            {
                Assert.Equal(activeWarning.Id, alert.RawMaterialId);
                Assert.Equal(StockAlertLevel.Warning, alert.Level);
            });
        Assert.Equal(activeCritical.Id, Assert.Single(criticalAlerts).RawMaterialId);
        Assert.DoesNotContain(repeatedAlerts, alert => alert.RawMaterialId == inactiveCritical.Id);
    }

    [Fact]
    public async Task Order_alerts_exclude_inactive_materials_referenced_by_bill_of_materials()
    {
        await using var dbContext = CreateDbContext();
        var orders = new OrderRepository(dbContext);
        var products = new ProductRepository(dbContext);
        var rawMaterials = new RawMaterialRepository(dbContext);
        var stockMovements = new StockMovementRepository(dbContext);
        var activeMaterial = new RawMaterial(
            Guid.NewGuid(),
            "Linha",
            "un",
            minimumStockLevel: 10,
            criticalStockLevel: 2);
        var inactiveMaterial = new RawMaterial(
            Guid.NewGuid(),
            "Tecido desativado",
            "m",
            RawMaterialStatus.Inactive,
            minimumStockLevel: 10,
            criticalStockLevel: 2);
        var product = new Product(
            Guid.NewGuid(),
            "Bolsa",
            billOfMaterials:
            [
                new BillOfMaterialsItem(activeMaterial.Id, 1),
                new BillOfMaterialsItem(inactiveMaterial.Id, 1)
            ]);
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "INACTIVE-ALERT-001"),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Bolsa", 1, new Money(10, "BRL"), product.Id)]);

        await rawMaterials.AddAsync(activeMaterial);
        await rawMaterials.AddAsync(inactiveMaterial);
        await products.AddAsync(product);
        await orders.AddAsync(order);
        await dbContext.SaveChangesAsync();

        var service = new StockAlertAppService(rawMaterials, stockMovements, orders, products);

        var alerts = await service.ListAlertsForOrderAsync(order.Id);

        Assert.Equal(activeMaterial.Id, Assert.Single(alerts).RawMaterialId);
        Assert.DoesNotContain(alerts, alert => alert.RawMaterialId == inactiveMaterial.Id);
    }

    [Fact]
    public async Task Saving_inactivation_removes_a_warmed_alert_after_the_commit()
    {
        await using var dbContext = CreateDbContext();
        var cache = new RecordingApplicationCache();
        var rawMaterials = new RawMaterialRepository(dbContext, cache);
        var stockMovements = new StockMovementRepository(dbContext, cache);
        var material = new RawMaterial(
            Guid.NewGuid(),
            "Linha",
            "un",
            minimumStockLevel: 10,
            criticalStockLevel: 2);

        await rawMaterials.AddAsync(material);
        await stockMovements.AddAsync(new StockMovement(
            Guid.NewGuid(), material.Id, StockMovementType.Inbound, 1, "Compra", null));
        await dbContext.SaveChangesAsync();

        var alertService = new StockAlertAppService(
            rawMaterials,
            stockMovements,
            new OrderRepository(dbContext),
            new ProductRepository(dbContext),
            cache);
        Assert.Equal(material.Id, Assert.Single(await alertService.ListAlertsAsync()).RawMaterialId);
        cache.ClearRemovedPrefixes();

        var inventoryService = new InventoryAppService(
            rawMaterials,
            stockMovements,
            new UnitOfWork(dbContext),
            cache);
        await inventoryService.SaveRawMaterialAsync(new Craftsman.App.Models.RawMaterialInputModel
        {
            Id = material.Id,
            Name = material.Name,
            UnitOfMeasure = material.UnitOfMeasure,
            Status = RawMaterialStatus.Inactive,
            MinimumStockLevel = material.MinimumStockLevel,
            CriticalStockLevel = material.CriticalStockLevel
        });

        Assert.Empty(await alertService.ListAlertsAsync());
        Assert.Equal(2, cache.RemovedPrefixes.Count(prefix => prefix == "inventory:"));
    }

    [Fact]
    public async Task Failed_material_save_does_not_run_post_commit_cache_invalidation()
    {
        await using var dbContext = CreateDbContext();
        var cache = new RecordingApplicationCache();
        var rawMaterials = new RawMaterialRepository(dbContext, cache);
        var material = new RawMaterial(Guid.NewGuid(), "Linha", "un");
        await rawMaterials.AddAsync(material);
        await dbContext.SaveChangesAsync();
        cache.ClearRemovedPrefixes();

        var service = new InventoryAppService(
            rawMaterials,
            new StockMovementRepository(dbContext, cache),
            new FailingUnitOfWork(),
            cache);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveRawMaterialAsync(new Craftsman.App.Models.RawMaterialInputModel
            {
                Id = material.Id,
                Name = material.Name,
                UnitOfMeasure = material.UnitOfMeasure,
                Status = RawMaterialStatus.Inactive
            }));

        Assert.Equal(["inventory:"], cache.RemovedPrefixes);
    }

    private static StockAlertAppService CreateService(
        AppDbContext dbContext,
        RawMaterialRepository rawMaterials,
        StockMovementRepository stockMovements)
    {
        return new StockAlertAppService(
            rawMaterials,
            stockMovements,
            new OrderRepository(dbContext),
            new ProductRepository(dbContext));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private sealed class FailingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Falha simulada ao salvar.");
        }
    }

    private sealed class RecordingApplicationCache : IApplicationCache
    {
        private readonly Dictionary<string, object> values = [];

        public List<string> RemovedPrefixes { get; } = [];

        public async Task<T> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan? absoluteExpirationRelativeToNow = null,
            CancellationToken cancellationToken = default)
        {
            if (values.TryGetValue(key, out var value))
            {
                return (T)value;
            }

            var created = await factory(cancellationToken);
            values[key] = created!;
            return created;
        }

        public void Remove(string key)
        {
            values.Remove(key);
        }

        public void RemoveByPrefix(string prefix)
        {
            RemovedPrefixes.Add(prefix);

            foreach (var key in values.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
            {
                values.Remove(key);
            }
        }

        public void ClearRemovedPrefixes()
        {
            RemovedPrefixes.Clear();
        }
    }
}
