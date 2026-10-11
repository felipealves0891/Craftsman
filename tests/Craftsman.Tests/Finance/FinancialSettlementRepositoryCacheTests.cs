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
