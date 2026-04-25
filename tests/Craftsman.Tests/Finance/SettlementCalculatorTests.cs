using Craftsman.Domain.Finance.Services;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Inventory.Services;
using Craftsman.Domain.ProductCatalog.Entities;
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
    public async Task Calculator_uses_revenue_and_consumed_material_cost_to_calculate_margin()
    {
        await using var dbContext = CreateDbContext();
        var rawMaterial = new RawMaterial(Guid.NewGuid(), "Tecido", "metro");
        var product = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(rawMaterial.Id, 2)]);
        var orderItem = new OrderItem(Guid.NewGuid(), "EXT-1", "Bolsa externa", 2, new Money(50, "BRL"), product.Id);
        var order = new Order(Guid.NewGuid(), new OrderOrigin("Elo7", "E-1"), new CustomerInfo("Ana", null), [orderItem], OrderStatus.Delivered);
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

        var calculator = new SettlementCalculator(orderRepository, productionTaskRepository, stockMovementRepository, shipmentRepository);

        var settlement = await calculator.CalculateAsync(order.Id);

        Assert.Equal(100, settlement.RevenueAmount);
        Assert.Equal(12, settlement.ProductionCostAmount);
        Assert.Equal(88, settlement.MarginAmount);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
