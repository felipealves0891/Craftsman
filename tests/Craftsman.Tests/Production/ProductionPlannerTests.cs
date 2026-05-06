using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Inventory.Services;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.Production.Entities;
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
        var product = new Product(Guid.NewGuid(), "Bolsa", productionDurationHours: 2, billOfMaterials: [new BillOfMaterialsItem(rawMaterial.Id, 2)]);
        var orderItem = new OrderItem(Guid.NewGuid(), "EXT-1", "Bolsa externa", 2, new Money(50, "BRL"), product.Id);
        var order = new Order(Guid.NewGuid(), new OrderOrigin("Elo7", "E-1"), [orderItem], shippingDate: new DateOnly(2026, 5, 10));

        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var inventoryService = new InventoryService(rawMaterialRepository, stockMovementRepository);
        var planner = new ProductionPlanner(productRepository, inventoryService, productionTaskRepository);

        await rawMaterialRepository.AddAsync(rawMaterial);
        await stockMovementRepository.AddAsync(new StockMovement(Guid.NewGuid(), rawMaterial.Id, StockMovementType.Inbound, 10, "Initial stock", null));
        await productRepository.AddAsync(product);
        await dbContext.SaveChangesAsync();

        var tasks = await planner.PlanAsync(order);
        await dbContext.SaveChangesAsync();

        var productionTask = Assert.Single(tasks);
        Assert.Equal(order.Id, productionTask.OrderId);
        Assert.Equal(product.Id, productionTask.ProductId);
        Assert.Equal(4, productionTask.ProductionDurationHours);
        Assert.Equal(new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero), productionTask.PlannedStartAt);
        Assert.Equal(new DateTimeOffset(2026, 5, 10, 14, 0, 0, TimeSpan.Zero), productionTask.PlannedAt);
        Assert.Equal(6, await stockMovementRepository.GetBalanceAsync(rawMaterial.Id));
    }

    [Fact]
    public async Task Planner_uses_shipping_date_and_quantity_to_schedule_by_hours()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = new RawMaterial(Guid.NewGuid(), "Linha", "un");
        var product = new Product(Guid.NewGuid(), "Kit", productionDurationHours: 2, billOfMaterials: [new BillOfMaterialsItem(rawMaterial.Id, 1)]);
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "M-1"),
            [new OrderItem(Guid.NewGuid(), "KIT", "Kit", 3, new Money(30, "BRL"), product.Id)],
            createdAt: new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero),
            shippingDate: new DateOnly(2026, 5, 10));
        var (planner, stockMovementRepository) = await CreatePlannerAsync(dbContext, rawMaterial, product, 10);

        var task = Assert.Single(await planner.PlanAsync(order));
        await dbContext.SaveChangesAsync();

        Assert.Equal(6, task.ProductionDurationHours);
        Assert.Equal(new DateTimeOffset(2026, 5, 10, 8, 0, 0, TimeSpan.Zero), task.PlannedStartAt);
        Assert.Equal(new DateTimeOffset(2026, 5, 10, 14, 0, 0, TimeSpan.Zero), task.PlannedAt);
        Assert.Equal(7, await stockMovementRepository.GetBalanceAsync(rawMaterial.Id));
    }

    [Fact]
    public async Task Planner_moves_to_previous_day_when_shipping_day_is_full()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = new RawMaterial(Guid.NewGuid(), "Couro", "m");
        var product = new Product(Guid.NewGuid(), "Carteira", productionDurationHours: 2, billOfMaterials: [new BillOfMaterialsItem(rawMaterial.Id, 1)]);
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "M-2"),
            [new OrderItem(Guid.NewGuid(), "CAR", "Carteira", 1, new Money(80, "BRL"), product.Id)],
            shippingDate: new DateOnly(2026, 5, 10));
        var (planner, _) = await CreatePlannerAsync(dbContext, rawMaterial, product, 10);
        var existingTask = new ProductionTask(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            product.Id,
            1,
            6,
            plannedStartAt: new DateTimeOffset(2026, 5, 10, 8, 0, 0, TimeSpan.Zero),
            plannedAt: new DateTimeOffset(2026, 5, 10, 14, 0, 0, TimeSpan.Zero));
        await new ProductionTaskRepository(dbContext).AddAsync(existingTask);
        await dbContext.SaveChangesAsync();

        var task = Assert.Single(await planner.PlanAsync(order));

        Assert.Equal(new DateTimeOffset(2026, 5, 9, 12, 0, 0, TimeSpan.Zero), task.PlannedStartAt);
        Assert.Equal(new DateTimeOffset(2026, 5, 9, 14, 0, 0, TimeSpan.Zero), task.PlannedAt);
    }

    [Fact]
    public async Task Planner_defaults_to_six_working_hours_per_day_when_not_configured()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = new RawMaterial(Guid.NewGuid(), "Madeira", "m");
        var product = new Product(Guid.NewGuid(), "Caixa", productionDurationHours: 7, billOfMaterials: [new BillOfMaterialsItem(rawMaterial.Id, 1)]);
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "M-3"),
            [new OrderItem(Guid.NewGuid(), "CX", "Caixa", 1, new Money(100, "BRL"), product.Id)],
            shippingDate: new DateOnly(2026, 5, 10));
        var (planner, _) = await CreatePlannerAsync(dbContext, rawMaterial, product, 10, new ProductionScheduleOptions());

        var task = Assert.Single(await planner.PlanAsync(order));

        Assert.Equal(new DateTimeOffset(2026, 5, 9, 13, 0, 0, TimeSpan.Zero), task.PlannedStartAt);
        Assert.Equal(new DateTimeOffset(2026, 5, 10, 14, 0, 0, TimeSpan.Zero), task.PlannedAt);
    }

    [Fact]
    public async Task Planner_uses_order_creation_date_when_shipping_date_is_missing()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = new RawMaterial(Guid.NewGuid(), "Papel", "fl");
        var product = new Product(Guid.NewGuid(), "Cartao", productionDurationHours: 1, billOfMaterials: [new BillOfMaterialsItem(rawMaterial.Id, 1)]);
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Import", "I-1"),
            [new OrderItem(Guid.NewGuid(), "CT", "Cartao", 1, new Money(10, "BRL"), product.Id)],
            createdAt: new DateTimeOffset(2026, 5, 8, 15, 0, 0, TimeSpan.Zero));
        var (planner, _) = await CreatePlannerAsync(dbContext, rawMaterial, product, 10);

        var task = Assert.Single(await planner.PlanAsync(order));

        Assert.Equal(new DateTimeOffset(2026, 5, 8, 13, 0, 0, TimeSpan.Zero), task.PlannedStartAt);
        Assert.Equal(new DateTimeOffset(2026, 5, 8, 14, 0, 0, TimeSpan.Zero), task.PlannedAt);
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

    private static async Task<(ProductionPlanner Planner, StockMovementRepository StockMovementRepository)> CreatePlannerAsync(
        AppDbContext dbContext,
        RawMaterial rawMaterial,
        Product product,
        decimal stockQuantity,
        ProductionScheduleOptions? options = null)
    {
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);

        await rawMaterialRepository.AddAsync(rawMaterial);
        await stockMovementRepository.AddAsync(new StockMovement(Guid.NewGuid(), rawMaterial.Id, StockMovementType.Inbound, stockQuantity, "Initial stock", null));
        await productRepository.AddAsync(product);
        await dbContext.SaveChangesAsync();

        return (
            new ProductionPlanner(
                productRepository,
                new InventoryService(rawMaterialRepository, stockMovementRepository),
                productionTaskRepository,
                options),
            stockMovementRepository);
    }
}
