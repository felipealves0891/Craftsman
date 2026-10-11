using Craftsman.App.Services;
using Craftsman.Domain.Events;
using Craftsman.Domain.Finance.Entities;
using Craftsman.Domain.Finance.Services;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Domain.Services;
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

    [Fact]
    public async Task Index_summary_returns_totals_for_selected_period()
    {
        await using var dbContext = CreateDbContext();
        await SeedSettlementAsync(dbContext, revenue: 100, productionCost: 25, shippingCost: 5, calculatedAt: new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero));
        await SeedSettlementAsync(dbContext, revenue: 80, productionCost: 30, shippingCost: 10, calculatedAt: new DateTimeOffset(2026, 10, 12, 12, 0, 0, TimeSpan.Zero));
        await SeedSettlementAsync(dbContext, revenue: 200, productionCost: 70, shippingCost: 20, calculatedAt: new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero));
        var service = CreateService(dbContext);

        var index = await service.GetIndexAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

        Assert.Equal(2, index.Summary.OrderCount);
        Assert.Equal(180, index.Summary.TotalRevenueAmount);
        Assert.Equal(70, index.Summary.TotalCostAmount);
        Assert.Equal(110, index.Summary.TotalMarginAmount);
        Assert.Equal(55, index.Summary.MarginPerOrderAmount);
        Assert.Equal(2, index.Settlements.Count);
    }

    [Fact]
    public async Task Index_summary_returns_zeroes_when_period_has_no_settlements()
    {
        await using var dbContext = CreateDbContext();
        await SeedSettlementAsync(dbContext, revenue: 100, productionCost: 25, shippingCost: 5, calculatedAt: new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero));
        var service = CreateService(dbContext);

        var index = await service.GetIndexAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

        Assert.Empty(index.Settlements);
        Assert.Equal(0, index.Summary.OrderCount);
        Assert.Equal(0, index.Summary.TotalRevenueAmount);
        Assert.Equal(0, index.Summary.TotalCostAmount);
        Assert.Equal(0, index.Summary.TotalMarginAmount);
        Assert.Equal(0, index.Summary.MarginPerOrderAmount);
    }

    [Fact]
    public async Task Summary_uses_cache_for_equivalent_period_for_one_hour()
    {
        await using var dbContext = CreateDbContext();
        await SeedSettlementAsync(dbContext, revenue: 100, productionCost: 25, shippingCost: 5, calculatedAt: new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero));
        var cache = new RecordingApplicationCache();
        var service = CreateService(dbContext, cache);

        await service.GetSummaryAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));
        await service.GetSummaryAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

        Assert.Equal(1, cache.FactoryCalls);
        Assert.Equal("finance:settlement-summary:20261001:20261031", cache.LastKey);
        Assert.Equal(TimeSpan.FromHours(1), cache.LastExpiration);
    }

    private static FinanceAppService CreateService(AppDbContext dbContext, IApplicationCache? cache = null)
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
            new NoOpDomainEventPublisher(),
            cache);
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

    private static async Task SeedSettlementAsync(
        AppDbContext dbContext,
        decimal revenue,
        decimal productionCost,
        decimal shippingCost,
        DateTimeOffset calculatedAt)
    {
        await new FinancialSettlementRepository(dbContext).AddAsync(new FinancialSettlement(
            Guid.NewGuid(),
            Guid.NewGuid(),
            revenue,
            productionCost,
            shippingCost,
            calculatedAt: calculatedAt,
            raiseCalculatedEvent: false));
        await dbContext.SaveChangesAsync();
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

    private sealed class RecordingApplicationCache : IApplicationCache
    {
        private readonly Dictionary<string, object> values = [];

        public int FactoryCalls { get; private set; }

        public TimeSpan? LastExpiration { get; private set; }

        public string? LastKey { get; private set; }

        public async Task<T> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan? absoluteExpirationRelativeToNow = null,
            CancellationToken cancellationToken = default)
        {
            LastKey = key;
            LastExpiration = absoluteExpirationRelativeToNow;

            if (values.TryGetValue(key, out var value))
            {
                return (T)value;
            }

            FactoryCalls++;
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
            foreach (var key in values.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
            {
                values.Remove(key);
            }
        }
    }
}
