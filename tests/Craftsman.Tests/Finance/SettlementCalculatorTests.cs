using Craftsman.Domain.Finance.Services;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Inventory.Services;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Production.Services;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Domain.Shipping.Entities;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Finance;

public sealed class SettlementCalculatorTests
{
    [Fact]
    public async Task Calculator_rejects_production_task_with_missing_product()
    {
        await using var dbContext = CreateDbContext();
        var productId = Guid.NewGuid();
        var orderItem = new OrderItem(Guid.NewGuid(), "ITEM-MISSING", "Produto ausente", 1, new Money(25, "BRL"), productId);
        var order = new Order(Guid.NewGuid(), new OrderOrigin("Manual", "MISSING-PRODUCT"), [orderItem], OrderStatus.Delivered);
        var orderRepository = new OrderRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);

        await orderRepository.AddAsync(order);
        await productionTaskRepository.AddAsync(new ProductionTask(
            Guid.NewGuid(),
            order.Id,
            orderItem.Id,
            productId,
            quantity: 1,
            productionDurationHours: 2));
        await dbContext.SaveChangesAsync();

        var calculator = new SettlementCalculator(
            orderRepository,
            productionTaskRepository,
            new StockMovementRepository(dbContext),
            new ShipmentRepository(dbContext),
            new ProductRepository(dbContext));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => calculator.CalculateAsync(order.Id));

        Assert.Contains(productId.ToString(), exception.Message);
    }

    [Fact]
    public async Task Calculator_uses_revenue_and_consumed_material_cost_to_calculate_margin()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = new RawMaterial(Guid.NewGuid(), "Tecido", "metro");
        var product = new Product(Guid.NewGuid(), "Bolsa", hourlyRate: 25, billOfMaterials: [new BillOfMaterialsItem(rawMaterial.Id, 2)]);
        var orderItem = new OrderItem(Guid.NewGuid(), "EXT-1", "Bolsa externa", 2, new Money(50, "BRL"), product.Id);
        var order = new Order(Guid.NewGuid(), new OrderOrigin("Elo7", "E-1"), [orderItem], OrderStatus.Delivered);
        var orderRepository = new OrderRepository(dbContext);
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var shipmentRepository = new ShipmentRepository(dbContext);
        var planner = new ProductionPlanner(productRepository, new InventoryService(rawMaterialRepository, stockMovementRepository));

        await orderRepository.AddAsync(order);
        await rawMaterialRepository.AddAsync(rawMaterial);
        await stockMovementRepository.AddAsync(new StockMovement(Guid.NewGuid(), rawMaterial.Id, StockMovementType.Inbound, 10, "Initial stock", null, unitCostAmount: 3));
        await productRepository.AddAsync(product);
        await dbContext.SaveChangesAsync();

        var productionTask = (await planner.PlanAsync(order)).Single();
        await productionTaskRepository.AddAsync(productionTask);
        await shipmentRepository.AddAsync(new Shipment(Guid.NewGuid(), order.Id, "TRACK-1", ShipmentStatus.Delivered, deliveredAt: DateTimeOffset.UtcNow));
        await dbContext.SaveChangesAsync();

        var calculator = new SettlementCalculator(orderRepository, productionTaskRepository, stockMovementRepository, shipmentRepository, productRepository);

        var settlement = await calculator.CalculateAsync(order.Id);

        Assert.Equal(100, settlement.RevenueAmount);
        Assert.Equal(62, settlement.ProductionCostAmount);
        Assert.Equal(50, settlement.LaborCostAmount);
        Assert.Equal(38, settlement.MarginAmount);
    }

    [Fact]
    public async Task Calculator_includes_order_discount_in_total_cost_and_margin()
    {
        await using var dbContext = CreateDbContext();
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "MAN-DISC-1"),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Bolsa", 2, new Money(50, "BRL"))],
            OrderStatus.Delivered,
            discount: new OrderDiscount("Desconto comercial", new Money(15, "BRL")));
        var orderRepository = new OrderRepository(dbContext);
        var calculator = new SettlementCalculator(
            orderRepository,
            new ProductionTaskRepository(dbContext),
            new StockMovementRepository(dbContext),
            new ShipmentRepository(dbContext),
            new ProductRepository(dbContext));

        await orderRepository.AddAsync(order);
        await dbContext.SaveChangesAsync();

        var settlement = await calculator.CalculateAsync(order.Id);

        Assert.Equal(100, settlement.RevenueAmount);
        Assert.Equal(15, settlement.DiscountAmount);
        Assert.Equal(15, settlement.TotalCostAmount);
        Assert.Equal(85, settlement.MarginAmount);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
