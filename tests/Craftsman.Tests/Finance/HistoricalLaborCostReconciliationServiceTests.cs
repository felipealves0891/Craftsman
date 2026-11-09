using Craftsman.App.Services;
using Craftsman.Domain.Finance.Entities;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Finance;

public sealed class HistoricalLaborCostReconciliationServiceTests
{
    [Fact]
    public async Task Reconciliation_updates_existing_settlement_without_double_counting_materials()
    {
        await using var dbContext = CreateDbContext();
        var product = new Product(Guid.NewGuid(), "Bolsa", hourlyRate: 25);
        var orderItem = new OrderItem(Guid.NewGuid(), "ITEM-1", "Bolsa", 1, new Money(100, "BRL"), product.Id);
        var order = new Order(Guid.NewGuid(), new OrderOrigin("Manual", "HIST-1"), [orderItem], OrderStatus.Delivered);
        var task = new ProductionTask(Guid.NewGuid(), order.Id, orderItem.Id, product.Id, 1, productionDurationHours: 10);
        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "metro");
        var movement = new StockMovement(Guid.NewGuid(), material.Id, StockMovementType.Outbound, 2, "Production", task.Id.ToString(), unitCostAmount: 150);
        var settlement = new FinancialSettlement(
            Guid.NewGuid(),
            order.Id,
            revenueAmount: 100,
            productionCostAmount: 300,
            shippingCostAmount: 0,
            raiseCalculatedEvent: false,
            laborCostReconciled: false);

        await new OrderRepository(dbContext).AddAsync(order);
        await new ProductRepository(dbContext).AddAsync(product);
        await new ProductionTaskRepository(dbContext).AddAsync(task);
        await new RawMaterialRepository(dbContext).AddAsync(material);
        await new StockMovementRepository(dbContext).AddAsync(movement);
        await new FinancialSettlementRepository(dbContext).AddAsync(settlement);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        await service.ReconcileAsync(settlement.Id);

        var loaded = await new FinancialSettlementRepository(dbContext).GetByIdAsync(settlement.Id);
        Assert.NotNull(loaded);
        Assert.Equal(250, loaded!.LaborCostAmount);
        Assert.Equal(550, loaded.ProductionCostAmount);
        Assert.Equal(550, loaded.TotalCostAmount);
        Assert.Equal(-450, loaded.MarginAmount);
        Assert.True(loaded.LaborCostReconciled);
    }

    [Fact]
    public async Task Reconciliation_is_idempotent_and_leaves_invalid_settlement_pending()
    {
        await using var dbContext = CreateDbContext();
        var product = new Product(Guid.NewGuid(), "Caneca", hourlyRate: 0);
        var orderItem = new OrderItem(Guid.NewGuid(), "ITEM-2", "Caneca", 1, new Money(50, "BRL"), product.Id);
        var order = new Order(Guid.NewGuid(), new OrderOrigin("Manual", "HIST-2"), [orderItem], OrderStatus.Delivered);
        var settlement = new FinancialSettlement(Guid.NewGuid(), order.Id, 50, 0, 0, raiseCalculatedEvent: false, laborCostReconciled: false);
        var invalidSettlement = new FinancialSettlement(Guid.NewGuid(), Guid.NewGuid(), 50, 0, 0, raiseCalculatedEvent: false, laborCostReconciled: false);

        await new OrderRepository(dbContext).AddAsync(order);
        await new ProductRepository(dbContext).AddAsync(product);
        await new FinancialSettlementRepository(dbContext).AddAsync(settlement);
        await new FinancialSettlementRepository(dbContext).AddAsync(invalidSettlement);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        await service.ReconcileAsync(settlement.Id);
        await service.ReconcileAsync(settlement.Id);

        var loaded = await new FinancialSettlementRepository(dbContext).GetByIdAsync(settlement.Id);
        Assert.NotNull(loaded);
        Assert.Equal(0, loaded!.LaborCostAmount);
        Assert.True(loaded.LaborCostReconciled);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReconcileAsync(invalidSettlement.Id));
        var pending = await new FinancialSettlementRepository(dbContext).ListPendingLaborCostReconciliationAsync();
        Assert.Contains(pending, item => item.Id == invalidSettlement.Id);
    }

    [Fact]
    public async Task Reconciliation_rejects_task_when_product_is_missing_without_updating_settlement()
    {
        await using var dbContext = CreateDbContext();
        var productId = Guid.NewGuid();
        var orderItem = new OrderItem(Guid.NewGuid(), "ITEM-3", "Produto", 1, new Money(100, "BRL"), productId);
        var order = new Order(Guid.NewGuid(), new OrderOrigin("Manual", "HIST-3"), [orderItem], OrderStatus.Delivered);
        var task = new ProductionTask(Guid.NewGuid(), order.Id, orderItem.Id, productId, 1, productionDurationHours: 2);
        var settlement = new FinancialSettlement(Guid.NewGuid(), order.Id, 100, 0, 0, raiseCalculatedEvent: false, laborCostReconciled: false);

        await new OrderRepository(dbContext).AddAsync(order);
        await new ProductionTaskRepository(dbContext).AddAsync(task);
        await new FinancialSettlementRepository(dbContext).AddAsync(settlement);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReconcileAsync(settlement.Id));

        Assert.Contains(productId.ToString(), exception.Message);
        var loaded = await new FinancialSettlementRepository(dbContext).GetByIdAsync(settlement.Id);
        Assert.NotNull(loaded);
        Assert.False(loaded!.LaborCostReconciled);
        Assert.Equal(0, loaded.LaborCostAmount);
    }

    private static HistoricalLaborCostReconciliationService CreateService(AppDbContext dbContext)
    {
        return new HistoricalLaborCostReconciliationService(
            new FinancialSettlementRepository(dbContext),
            new OrderRepository(dbContext),
            new ProductionTaskRepository(dbContext),
            new StockMovementRepository(dbContext),
            new ProductRepository(dbContext),
            new InlineTransactionalUnitOfWork(),
            new UnitOfWork(dbContext));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private sealed class InlineTransactionalUnitOfWork : ITransactionalUnitOfWork
    {
        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
        {
            return operation();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }
    }
}
