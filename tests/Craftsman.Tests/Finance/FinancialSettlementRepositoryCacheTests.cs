using Craftsman.Domain.Finance.Entities;
using Craftsman.Domain.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Finance;

public sealed class FinancialSettlementRepositoryCacheTests
{
    [Fact]
    public async Task List_uses_finance_cache_key_with_one_hour_expiration()
    {
        await using var dbContext = CreateDbContext();
        var cache = new RecordingApplicationCache();
        var repository = new FinancialSettlementRepository(dbContext, cache);

        await repository.ListAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

        Assert.Equal("finance:settlements:20261001:20261031", cache.LastKey);
        Assert.Equal(TimeSpan.FromHours(1), cache.LastExpiration);
    }

    [Fact]
    public async Task Add_removes_all_finance_cache_entries()
    {
        await using var dbContext = CreateDbContext();
        var cache = new RecordingApplicationCache();
        var repository = new FinancialSettlementRepository(dbContext, cache);

        await repository.ListAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));
        await repository.AddAsync(new FinancialSettlement(Guid.NewGuid(), Guid.NewGuid(), 100, 25, 5));

        Assert.Contains("finance:", cache.RemovedPrefixes);
    }

    [Fact]
    public async Task Add_and_get_preserve_labor_cost_amount()
    {
        await using var dbContext = CreateDbContext();
        var orderId = Guid.NewGuid();
        var repository = new FinancialSettlementRepository(dbContext);
        var settlement = new FinancialSettlement(
            Guid.NewGuid(),
            orderId,
            revenueAmount: 100,
            productionCostAmount: 35,
            shippingCostAmount: 5,
            laborCostAmount: 15,
            raiseCalculatedEvent: false);

        await repository.AddAsync(settlement);
        await dbContext.SaveChangesAsync();

        var loaded = await repository.GetByOrderIdAsync(orderId);

        Assert.NotNull(loaded);
        Assert.Equal(15, loaded!.LaborCostAmount);
        Assert.Equal(40, loaded.TotalCostAmount);
    }

    [Fact]
    public async Task Pending_query_returns_only_unreconciled_settlements()
    {
        await using var dbContext = CreateDbContext();
        var repository = new FinancialSettlementRepository(dbContext);
        var pending = new FinancialSettlement(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100,
            25,
            5,
            raiseCalculatedEvent: false,
            laborCostReconciled: false);
        var reconciled = new FinancialSettlement(Guid.NewGuid(), Guid.NewGuid(), 100, 25, 5);

        await repository.AddAsync(pending);
        await repository.AddAsync(reconciled);
        await dbContext.SaveChangesAsync();

        var result = await repository.ListPendingLaborCostReconciliationAsync();

        var loaded = Assert.Single(result);
        Assert.Equal(pending.Id, loaded.Id);
        Assert.False(loaded.LaborCostReconciled);
    }

    [Fact]
    public async Task Update_preserves_settlement_identity_and_invalidates_finance_cache()
    {
        await using var dbContext = CreateDbContext();
        var cache = new RecordingApplicationCache();
        var repository = new FinancialSettlementRepository(dbContext, cache);
        var calculatedAt = DateTimeOffset.Parse("2026-10-15T12:00:00+00:00");
        var pending = new FinancialSettlement(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1000,
            300,
            10,
            discountAmount: 5,
            calculatedAt: calculatedAt,
            raiseCalculatedEvent: false,
            laborCostReconciled: false);

        await repository.AddAsync(pending);
        await dbContext.SaveChangesAsync();
        pending.ReconcileLaborCost(250, 550);
        await repository.UpdateAsync(pending);
        await dbContext.SaveChangesAsync();

        var loaded = await repository.GetByOrderIdAsync(pending.OrderId);
        Assert.NotNull(loaded);
        Assert.Equal(pending.Id, loaded!.Id);
        Assert.Equal(pending.OrderId, loaded.OrderId);
        Assert.Equal(calculatedAt, loaded.CalculatedAt);
        Assert.Equal(250, loaded.LaborCostAmount);
        Assert.Equal(550, loaded.ProductionCostAmount);
        Assert.True(loaded.LaborCostReconciled);
        Assert.Contains("finance:", cache.RemovedPrefixes);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private sealed class RecordingApplicationCache : IApplicationCache
    {
        public TimeSpan? LastExpiration { get; private set; }

        public string? LastKey { get; private set; }

        public List<string> RemovedPrefixes { get; } = [];

        public async Task<T> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan? absoluteExpirationRelativeToNow = null,
            CancellationToken cancellationToken = default)
        {
            LastKey = key;
            LastExpiration = absoluteExpirationRelativeToNow;
            return await factory(cancellationToken);
        }

        public void Remove(string key)
        {
        }

        public void RemoveByPrefix(string prefix)
        {
            RemovedPrefixes.Add(prefix);
        }
    }
}
