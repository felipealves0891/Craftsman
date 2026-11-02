using Craftsman.Domain.Events;
using Craftsman.Domain.Shipping.Entities;
using Craftsman.Infra.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Craftsman.Tests.Integration;

public sealed class InfrastructureServiceTests
{
    [Fact]
    public async Task Memory_cache_reuses_entries_and_removes_keys_by_name_and_prefix()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new MemoryApplicationCache(memoryCache, CreateConfiguration(enabled: true));
        var factoryCalls = 0;
        Task<string> Create(CancellationToken _) => Task.FromResult($"value-{++factoryCalls}");

        Assert.Equal("value-1", await cache.GetOrCreateAsync("finance:1", Create));
        Assert.Equal("value-1", await cache.GetOrCreateAsync("finance:1", Create, TimeSpan.FromMinutes(2)));
        Assert.Equal("value-2", await cache.GetOrCreateAsync("orders:1", Create));
        Assert.Equal("value-3", await cache.GetOrCreateAsync("orders:2", Create));
        Assert.Equal("value-4", await cache.GetOrCreateAsync("other:1", Create));
        Assert.Equal("value-5", await cache.GetOrCreateAsync("explicit-expiration", Create, TimeSpan.FromMinutes(1)));

        cache.Remove("finance:1");
        cache.Remove("not-cached");
        cache.RemoveByPrefix("orders:");
        cache.RemoveByPrefix("missing:");

        Assert.Equal("value-6", await cache.GetOrCreateAsync("finance:1", Create));
        Assert.Equal("value-7", await cache.GetOrCreateAsync("orders:1", Create));
        Assert.Equal("value-8", await cache.GetOrCreateAsync("orders:2", Create));
        Assert.Equal(8, factoryCalls);
    }

    [Fact]
    public async Task Disabled_memory_cache_invokes_factory_for_each_request()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new MemoryApplicationCache(memoryCache, CreateConfiguration(enabled: false));
        var factoryCalls = 0;

        Task<int> Create(CancellationToken _) => Task.FromResult(++factoryCalls);

        Assert.Equal(1, await cache.GetOrCreateAsync("same", Create));
        Assert.Equal(2, await cache.GetOrCreateAsync("same", Create));
        Assert.Equal(2, factoryCalls);
    }

    [Fact]
    public async Task In_memory_event_publisher_handles_zero_and_multiple_handlers()
    {
        var noHandlers = new InMemoryDomainEventPublisher(new ServiceCollection().BuildServiceProvider());
        await noHandlers.PublishAsync(new TestEvent());

        var services = new ServiceCollection();
        var first = new RecordingHandler();
        var second = new RecordingHandler();
        services.AddSingleton<IDomainEventHandler<TestEvent>>(first);
        services.AddSingleton<IDomainEventHandler<TestEvent>>(second);
        var publisher = new InMemoryDomainEventPublisher(services.BuildServiceProvider());
        var domainEvent = new TestEvent();

        await publisher.PublishAsync(domainEvent);

        Assert.Same(domainEvent, Assert.Single(first.Events));
        Assert.Same(domainEvent, Assert.Single(second.Events));
    }

    [Fact]
    public async Task Static_shipping_tracker_ignores_blank_codes_and_trims_valid_codes()
    {
        var tracker = new StaticShippingTracker();

        Assert.Null(await tracker.TrackAsync(" "));
        Assert.Null(await tracker.TrackAsync("\t"));

        var tracked = await tracker.TrackAsync("  TRACK-1  ");

        Assert.NotNull(tracked);
        Assert.Equal("TRACK-1", tracked!.TrackingCode);
        Assert.Equal(ShipmentStatus.InTransit, tracked.Status);
        Assert.True(tracked.TrackedAt <= DateTimeOffset.UtcNow);
    }

    private static IConfiguration CreateConfiguration(bool enabled)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Cache:Enabled"] = enabled.ToString() })
            .Build();

    private sealed record TestEvent : DomainEvent;

    private sealed class RecordingHandler : IDomainEventHandler<TestEvent>
    {
        public List<TestEvent> Events { get; } = [];

        public Task HandleAsync(TestEvent domainEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(domainEvent);
            return Task.CompletedTask;
        }
    }
}
