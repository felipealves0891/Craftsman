using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Inventory.Services;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.Production.Services;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Production;

public sealed class ProductionPlannerTests
{
    [Fact]
    public async Task Planner_creates_task_and_consumes_stock_when_material_is_available()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = new RawMaterial(Guid.NewGuid(), "Tecido", "metro");
        var product = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(rawMaterial.Id, 2)]);
        var orderItem = new OrderItem(Guid.NewGuid(), "EXT-1", "Bolsa externa", 2, new Money(50, "BRL"), product.Id);
        var order = new Order(Guid.NewGuid(), new OrderOrigin("Elo7", "E-1"), [orderItem]);

        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var inventoryService = new InventoryService(rawMaterialRepository, stockMovementRepository);
        var planner = new ProductionPlanner(productRepository, inventoryService);

        await rawMaterialRepository.AddAsync(rawMaterial);
        await stockMovementRepository.AddAsync(new StockMovement(Guid.NewGuid(), rawMaterial.Id, StockMovementType.Inbound, 10, "Initial stock", null));
        await productRepository.AddAsync(product);
        await dbContext.SaveChangesAsync();

        var tasks = await planner.PlanAsync(order);
        await dbContext.SaveChangesAsync();

        var productionTask = Assert.Single(tasks);
        Assert.Equal(order.Id, productionTask.OrderId);
        Assert.Equal(product.Id, productionTask.ProductId);
        Assert.Equal(6, await stockMovementRepository.GetBalanceAsync(rawMaterial.Id));
    }

    [Fact]
    public async Task Planner_rejects_order_item_without_product_mapping()
    {
        await using var dbContext = CreateDbContext();
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Elo7", "E-2"),
            [new OrderItem(Guid.NewGuid(), "EXT-1", "Bolsa externa", 2, new Money(50, "BRL"))]);
        var planner = new ProductionPlanner(
            new ProductRepository(dbContext),
            new InventoryService(new RawMaterialRepository(dbContext), new StockMovementRepository(dbContext)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => planner.PlanAsync(order));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
