using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Inventory.Services;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Production.Repositories;
using Craftsman.Domain.Production.Services;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Production;

public sealed class ProductionPreflightTests
{
    [Fact]
    public async Task Preflight_lists_required_available_and_missing_quantities()
    {
        await using var db = CreateDbContext();
        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "m");
        var product = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(material.Id, 4)]);
        var order = CreateOrder(product.Id);
        var service = await CreateServiceAsync(db, material, product, order, 3);

        var result = await service.GetAsync(order.Id);

        var row = Assert.Single(result.Materials);
        Assert.False(result.CanConfirm);
        Assert.Equal("Tecido", row.Name);
        Assert.Equal("m", row.UnitOfMeasure);
        Assert.Equal(4m, row.RequiredQuantity);
        Assert.Equal(3m, row.AvailableQuantity);
        Assert.Equal(1m, row.MissingQuantity);
        Assert.False(row.IsAvailable);
    }

    [Fact]
    public async Task Preflight_allows_confirmation_when_stock_is_exactly_sufficient()
    {
        await using var db = CreateDbContext();
        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "m");
        var product = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(material.Id, 4)]);
        var order = CreateOrder(product.Id);
        var service = await CreateServiceAsync(db, material, product, order, 4);

        var result = await service.GetAsync(order.Id);

        Assert.True(result.CanConfirm);
        Assert.True(Assert.Single(result.Materials).IsAvailable);
        Assert.Equal(0m, Assert.Single(result.Materials).MissingQuantity);
    }

    [Fact]
    public async Task Preflight_preserves_product_material_breakdown_and_consolidated_availability()
    {
        await using var db = CreateDbContext();
        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "m");
        var bag = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(material.Id, 2)]);
        var caseProduct = new Product(Guid.NewGuid(), "Estojo", billOfMaterials: [new BillOfMaterialsItem(material.Id, 3)]);
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "PREFLIGHT-MULTI-001"),
            [
                new OrderItem(Guid.NewGuid(), "ITEM-BAG", "Bolsa", 2, new Money(10, "BRL"), bag.Id),
                new OrderItem(Guid.NewGuid(), "ITEM-CASE", "Estojo", 1, new Money(8, "BRL"), caseProduct.Id)
            ]);
        var materialRepository = new RawMaterialRepository(db);
        var movementRepository = new StockMovementRepository(db);
        var productRepository = new ProductRepository(db);
        var orderRepository = new OrderRepository(db);
        await materialRepository.AddAsync(material);
        await productRepository.AddAsync(bag);
        await productRepository.AddAsync(caseProduct);
        await orderRepository.AddAsync(order);
        await movementRepository.AddAsync(new StockMovement(
            Guid.NewGuid(), material.Id, StockMovementType.Inbound, 7, "Initial stock", null));
        await db.SaveChangesAsync();

        var service = new ProductionPreflightService(
            orderRepository,
            new ProductionTaskRepository(db),
            new ProductionPlanPreparationService(productRepository),
            new InventoryService(materialRepository, movementRepository));

        var result = await service.GetAsync(order.Id);

        Assert.True(result.CanConfirm);
        Assert.Equal(2, result.Products.Count);
        Assert.Equal(new[] { 4m, 3m }, result.Products.SelectMany(product => product.Materials).Select(materialRow => materialRow.RequiredQuantity));
        var consolidated = Assert.Single(result.Materials);
        Assert.Equal(7m, consolidated.RequiredQuantity);
        Assert.Equal(7m, consolidated.AvailableQuantity);
        Assert.True(consolidated.IsAvailable);
    }

    [Fact]
    public async Task Preflight_blocks_inactive_material_as_configuration_error()
    {
        await using var db = CreateDbContext();
        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "m", RawMaterialStatus.Inactive);
        var product = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(material.Id, 4)]);
        var order = CreateOrder(product.Id);
        var service = await CreateServiceAsync(db, material, product, order, 4);

        var result = await service.GetAsync(order.Id);

        var row = Assert.Single(result.Materials);
        Assert.False(result.CanConfirm);
        Assert.False(row.IsConfigured);
    }

    [Fact]
    public async Task Preflight_reports_missing_order()
    {
        await using var db = CreateDbContext();
        var service = new ProductionPreflightService(
            new OrderRepository(db),
            new ProductionTaskRepository(db),
            new ProductionPlanPreparationService(new ProductRepository(db)),
            new InventoryService(new RawMaterialRepository(db), new StockMovementRepository(db)));

        var result = await service.GetAsync(Guid.NewGuid());

        Assert.False(result.CanConfirm);
        Assert.Equal(ProductionPlanningFailureReason.OrderNotFound, result.FailureReason);
        Assert.Empty(result.Materials);
    }

    [Fact]
    public async Task Preflight_reports_already_planned_order()
    {
        await using var db = CreateDbContext();
        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "m");
        var product = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(material.Id, 4)]);
        var order = CreateOrder(product.Id);
        var service = await CreateServiceAsync(db, material, product, order, 4);
        await new ProductionTaskRepository(db).AddAsync(new ProductionTask(
            Guid.NewGuid(), order.Id, order.Items.Single().Id, product.Id, 1, 1));
        await db.SaveChangesAsync();

        var result = await service.GetAsync(order.Id);

        Assert.False(result.CanConfirm);
        Assert.Equal(ProductionPlanningFailureReason.AlreadyPlanned, result.FailureReason);
    }

    [Fact]
    public async Task Preflight_reports_missing_raw_material_configuration()
    {
        await using var db = CreateDbContext();
        var missingMaterialId = Guid.NewGuid();
        var product = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(missingMaterialId, 4)]);
        var order = CreateOrder(product.Id);
        var productRepository = new ProductRepository(db);
        var orderRepository = new OrderRepository(db);
        await productRepository.AddAsync(product);
        await orderRepository.AddAsync(order);
        await db.SaveChangesAsync();

        var service = new ProductionPreflightService(
            orderRepository,
            new ProductionTaskRepository(db),
            new ProductionPlanPreparationService(productRepository),
            new InventoryService(new RawMaterialRepository(db), new StockMovementRepository(db)));

        var result = await service.GetAsync(order.Id);

        var row = Assert.Single(result.Materials);
        Assert.False(result.CanConfirm);
        Assert.False(row.IsConfigured);
        Assert.Equal("Matéria-prima não cadastrada", row.Name);
        Assert.Equal("-", row.UnitOfMeasure);
    }

    private static async Task<ProductionPreflightService> CreateServiceAsync(
        AppDbContext db,
        RawMaterial material,
        Product product,
        Order order,
        decimal stock)
    {
        var materialRepository = new RawMaterialRepository(db);
        var movementRepository = new StockMovementRepository(db);
        var productRepository = new ProductRepository(db);
        var orderRepository = new OrderRepository(db);
        var taskRepository = new ProductionTaskRepository(db);

        await materialRepository.AddAsync(material);
        await productRepository.AddAsync(product);
        await orderRepository.AddAsync(order);
        await movementRepository.AddAsync(new StockMovement(
            Guid.NewGuid(), material.Id, StockMovementType.Inbound, stock, "Initial stock", null));
        await db.SaveChangesAsync();

        return new ProductionPreflightService(
            orderRepository,
            taskRepository,
            new ProductionPlanPreparationService(productRepository),
            new InventoryService(materialRepository, movementRepository));
    }

    private static Order CreateOrder(Guid productId) => new(
        Guid.NewGuid(),
        new OrderOrigin("Manual", "PREFLIGHT-001"),
        [new OrderItem(Guid.NewGuid(), "ITEM-1", "Bolsa", 1, new Money(10, "BRL"), productId)]);

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
