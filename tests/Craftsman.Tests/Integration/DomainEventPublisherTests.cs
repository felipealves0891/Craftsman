using Craftsman.Domain.Events;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Craftsman.Tests.Integration;

public sealed class DomainEventPublisherTests
{
    [Fact]
    public async Task Publisher_resolves_handlers_by_runtime_event_type()
    {
        var handler = new RecordingDeliveryHandler();
        await using var provider = CreateServiceProvider(handler);
        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        IDomainEvent domainEvent = new DeliveryConfirmedEvent(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        await publisher.PublishAsync(domainEvent);
        await dbContext.SaveChangesAsync();

        var execution = await dbContext.DomainEventHandlerExecutions.SingleAsync();
        Assert.Equal(nameof(DeliveryConfirmedEvent), execution.EventName);
        Assert.Equal(DomainEventHandlerExecutionStatus.Succeeded.ToString(), execution.Status);
        Assert.Equal(1, handler.HandledCount);
    }

    [Fact]
    public async Task Publisher_records_failed_handler_execution_without_losing_event()
    {
        var handler = new RecordingDeliveryHandler { ShouldFail = true };
        await using var provider = CreateServiceProvider(handler);
        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await publisher.PublishAsync(new DeliveryConfirmedEvent(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow));
        await dbContext.SaveChangesAsync();

        var persistedEvent = await dbContext.DomainEvents.SingleAsync();
        var execution = await dbContext.DomainEventHandlerExecutions.SingleAsync();
        Assert.Equal(nameof(DeliveryConfirmedEvent), persistedEvent.EventName);
        Assert.Equal(DomainEventHandlerExecutionStatus.Failed.ToString(), execution.Status);
        Assert.Contains("forced failure", execution.LastError);
    }

    [Fact]
    public async Task Retry_service_reexecutes_failed_handler()
    {
        var handler = new RecordingDeliveryHandler { ShouldFail = true };
        await using var provider = CreateServiceProvider(handler);
        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await publisher.PublishAsync(new DeliveryConfirmedEvent(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow));
        await dbContext.SaveChangesAsync();
        handler.ShouldFail = false;

        var retriedCount = await scope.ServiceProvider.GetRequiredService<DomainEventRetryService>().RetryFailedAsync();

        var execution = await dbContext.DomainEventHandlerExecutions.SingleAsync();
        Assert.Equal(1, retriedCount);
        Assert.Equal(2, execution.AttemptCount);
        Assert.Equal(DomainEventHandlerExecutionStatus.Succeeded.ToString(), execution.Status);
    }

    private static ServiceProvider CreateServiceProvider(RecordingDeliveryHandler handler)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddScoped<IDomainEventPublisher, PersistentDomainEventPublisher>();
        services.AddScoped<DomainEventRetryService>();
        services.AddSingleton<IDomainEventHandler<DeliveryConfirmedEvent>>(handler);
        return services.BuildServiceProvider();
    }

    private sealed class RecordingDeliveryHandler : IDomainEventHandler<DeliveryConfirmedEvent>
    {
        public int HandledCount { get; private set; }

        public bool ShouldFail { get; set; }

        public Task HandleAsync(DeliveryConfirmedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            HandledCount++;

            if (ShouldFail)
            {
                throw new InvalidOperationException("forced failure");
            }

            return Task.CompletedTask;
        }
    }
}
