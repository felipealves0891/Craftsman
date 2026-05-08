using System.Text.Json;
using Craftsman.Domain.Events;
using Craftsman.Infra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Craftsman.Infra.Services;

public sealed class DomainEventRetryService
{
    private readonly AppDbContext dbContext;
    private readonly ILogger<DomainEventRetryService> logger;
    private readonly IServiceProvider serviceProvider;

    public DomainEventRetryService(
        IServiceProvider serviceProvider,
        AppDbContext dbContext,
        ILogger<DomainEventRetryService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<int> RetryFailedAsync(CancellationToken cancellationToken = default)
    {
        var failedExecutions = await dbContext.DomainEventHandlerExecutions
            .Include(execution => execution.DomainEvent)
            .Where(execution => execution.Status == DomainEventHandlerExecutionStatus.Failed.ToString())
            .OrderBy(execution => execution.CreatedAt)
            .ToListAsync(cancellationToken);

        var retriedCount = 0;
        foreach (var execution in failedExecutions)
        {
            var persistedEvent = execution.DomainEvent;
            if (persistedEvent is null)
            {
                continue;
            }

            var eventType = ResolveEventType(execution.EventName);
            var domainEvent = JsonSerializer.Deserialize(persistedEvent.Payload, eventType) as IDomainEvent
                ?? throw new InvalidOperationException($"Could not deserialize domain event {execution.EventName}.");
            var handlerContractType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handler = serviceProvider
                .GetServices(handlerContractType)
                .OfType<object>()
                .FirstOrDefault(candidate => (candidate.GetType().FullName ?? candidate.GetType().Name) == execution.HandlerName);

            if (handler is null)
            {
                execution.AttemptCount++;
                execution.LastError = $"Handler {execution.HandlerName} was not registered.";
                execution.StartedAt = DateTimeOffset.UtcNow;
                execution.CompletedAt = DateTimeOffset.UtcNow;
                logger.LogWarning("Domain event handler {HandlerName} was not registered for retry.", execution.HandlerName);
                continue;
            }

            execution.AttemptCount++;
            execution.Status = DomainEventHandlerExecutionStatus.Pending.ToString();
            execution.StartedAt = DateTimeOffset.UtcNow;
            execution.CompletedAt = null;

            try
            {
                await PersistentDomainEventPublisher.InvokeHandlerAsync(handlerContractType, handler, domainEvent, cancellationToken);
                execution.Status = DomainEventHandlerExecutionStatus.Succeeded.ToString();
                execution.LastError = null;
                retriedCount++;
            }
            catch (Exception exception)
            {
                execution.Status = DomainEventHandlerExecutionStatus.Failed.ToString();
                execution.LastError = exception.Message;
                logger.LogError(exception, "Retry failed for domain event execution {ExecutionId}.", execution.Id);
            }
            finally
            {
                execution.CompletedAt = DateTimeOffset.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return retriedCount;
    }

    private static Type ResolveEventType(string eventName)
    {
        return typeof(IDomainEvent).Assembly
            .GetTypes()
            .FirstOrDefault(type => typeof(IDomainEvent).IsAssignableFrom(type) && type.Name == eventName)
            ?? throw new InvalidOperationException($"Domain event type {eventName} was not found.");
    }
}
