using Craftsman.App.Services;
using Craftsman.Domain.Events;
using Craftsman.Domain.Finance.Services;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Domain.Shipping.Entities;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Finance;

public sealed class FinanceAppServiceTests
{
    [Fact]
    public async Task Pending_list_returns_delivered_orders_without_settlement()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedDeliveredOrderAsync(dbContext);
        var service = CreateService(dbContext);

        var pending = await service.ListPendingAsync();

        var item = Assert.Single(pending);
        Assert.Equal(order.Id, item.OrderId);
        Assert.Equal("Manual", item.Source);
        Assert.Equal("PED-FIN-1", item.ExternalOrderId);
    }

    [Fact]
    public async Task Manual_generation_creates_settlement_for_delivered_order()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedDeliveredOrderAsync(dbContext);
        var service = CreateService(dbContext);

        var settlement = await service.GenerateAsync(order.Id);

        Assert.Equal(order.Id, settlement.OrderId);
        Assert.Equal(50, settlement.RevenueAmount);
        Assert.Single(await dbContext.FinancialSettlements.ToListAsync());
    }

    [Fact]
    public async Task Manual_generation_is_idempotent_when_settlement_exists()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedDeliveredOrderAsync(dbContext);
        var service = CreateService(dbContext);

        await service.GenerateAsync(order.Id);
        await service.GenerateAsync(order.Id);

        Assert.Single(await dbContext.FinancialSettlements.ToListAsync());
    }

    private static FinanceAppService CreateService(AppDbContext dbContext)
    {
        var settlementRepository = new FinancialSettlementRepository(dbContext);
        var shipmentRepository = new ShipmentRepository(dbContext);
        var orderRepository = new OrderRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);

        return new FinanceAppService(
            settlementRepository,
            new SettlementCalculator(orderRepository, productionTaskRepository, stockMovementRepository, shipmentRepository),
            shipmentRepository,
            orderRepository,
            new UnitOfWork(dbContext),
            new NoOpDomainEventPublisher());
    }

    private static async Task<Order> SeedDeliveredOrderAsync(AppDbContext dbContext)
    {
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "PED-FIN-1"),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Bolsa", 2, new Money(25, "BRL"))]);

        await new OrderRepository(dbContext).AddAsync(order);
        await new ShipmentRepository(dbContext).AddAsync(new Shipment(
            Guid.NewGuid(),
            order.Id,
            "TRACK-FIN-1",
            ShipmentStatus.Delivered,
            deliveredAt: DateTimeOffset.UtcNow));
        await dbContext.SaveChangesAsync();
        return order;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private sealed class NoOpDomainEventPublisher : IDomainEventPublisher
    {
        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent
        {
            return Task.CompletedTask;
        }
    }
}
