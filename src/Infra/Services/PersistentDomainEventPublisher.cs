using System.Reflection;
using System.Text.Json;
using Craftsman.Domain.Events;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Craftsman.Infra.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Craftsman.Infra.Services;

public sealed class PersistentDomainEventPublisher : IDomainEventPublisher
{
    private readonly AppDbContext dbContext;
    private readonly ICurrentUserContext? currentUserContext;
    private readonly ILogger<PersistentDomainEventPublisher> logger;
    private readonly IServiceProvider serviceProvider;

    public PersistentDomainEventPublisher(
        IServiceProvider serviceProvider,
        AppDbContext dbContext,
        ILogger<PersistentDomainEventPublisher> logger,
        ICurrentUserContext? currentUserContext = null)
    {
        this.serviceProvider = serviceProvider;
        this.dbContext = dbContext;
        this.logger = logger;
        this.currentUserContext = currentUserContext;
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        if (domainEvent is null)
        {
            throw new ArgumentNullException(nameof(domainEvent));
        }

        await EnsurePersistedAsync(domainEvent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await ExecuteHandlersAsync(domainEvent, cancellationToken);
    }

    private async Task EnsurePersistedAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (await dbContext.DomainEvents.AnyAsync(persisted => persisted.Id == domainEvent.EventId, cancellationToken))
        {
            return;
        }

        var currentUser = currentUserContext?.Current ?? CurrentUserInfo.Anonymous();
        await dbContext.DomainEvents.AddAsync(new PersistedDomainEventEntity
        {
            Id = domainEvent.EventId,
            EventName = domainEvent.GetType().Name,
            OccurredAt = domainEvent.OccurredAt,
            Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            UserId = currentUser.UserId,
            UserName = currentUser.UserName,
            CorrelationId = currentUser.CorrelationId
        }, cancellationToken);
    }

    private async Task ExecuteHandlersAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();
        var handlerContractType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var handlers = serviceProvider.GetServices(handlerContractType).OfType<object>().ToList();

        foreach (var handler in handlers)
        {
            await ExecuteHandlerAsync(domainEvent, eventType, handlerContractType, handler, cancellationToken);
        }
    }

    private async Task ExecuteHandlerAsync(
        IDomainEvent domainEvent,
        Type eventType,
        Type handlerContractType,
        object handler,
        CancellationToken cancellationToken)
    {
        var execution = new DomainEventHandlerExecutionEntity
        {
            Id = Guid.NewGuid(),
            DomainEventId = domainEvent.EventId,
            EventName = eventType.Name,
            HandlerName = handler.GetType().FullName ?? handler.GetType().Name,
            Status = DomainEventHandlerExecutionStatus.Pending.ToString(),
            AttemptCount = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            StartedAt = DateTimeOffset.UtcNow
        };

        await dbContext.DomainEventHandlerExecutions.AddAsync(execution, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await InvokeHandlerAsync(handlerContractType, handler, domainEvent, cancellationToken);
            execution.Status = DomainEventHandlerExecutionStatus.Succeeded.ToString();
            execution.CompletedAt = DateTimeOffset.UtcNow;
            execution.LastError = null;
        }
        catch (Exception exception)
        {
            execution.Status = DomainEventHandlerExecutionStatus.Failed.ToString();
            execution.CompletedAt = DateTimeOffset.UtcNow;
            execution.LastError = exception.Message;
            logger.LogError(
                exception,
                "Domain event handler {HandlerName} failed for event {EventName}<{EventId}>.",
                execution.HandlerName,
                execution.EventName,
                execution.DomainEventId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    internal static async Task InvokeHandlerAsync(
        Type handlerContractType,
        object handler,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var method = handlerContractType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))
            ?? throw new InvalidOperationException($"Handler contract {handlerContractType.Name} does not expose HandleAsync.");

        try
        {
            var task = method.Invoke(handler, [domainEvent, cancellationToken]) as Task
                ?? throw new InvalidOperationException($"Handler {handler.GetType().Name} did not return a Task.");
            await task;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw exception.InnerException;
        }
    }
}
