using System.Text.Json;
using Craftsman.Domain.Events;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;

namespace Craftsman.Infra.Services;

public sealed class DomainEventPersistenceHandler<TEvent> : IDomainEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    private readonly AppDbContext dbContext;

    public DomainEventPersistenceHandler(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var persistedEvent = new PersistedDomainEventEntity
        {
            Id = domainEvent.EventId,
            EventName = domainEvent.GetType().Name,
            OccurredAt = domainEvent.OccurredAt,
            Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType())
        };

        await dbContext.DomainEvents.AddAsync(persistedEvent, cancellationToken);
    }
}
