using System.Text.Json;
using Craftsman.Domain.Events;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Craftsman.Infra.Security;

namespace Craftsman.Infra.Services;

public sealed class DomainEventPersistenceHandler<TEvent> : IDomainEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    private readonly AppDbContext dbContext;
    private readonly ICurrentUserContext? currentUserContext;

    public DomainEventPersistenceHandler(AppDbContext dbContext, ICurrentUserContext? currentUserContext = null)
    {
        this.dbContext = dbContext;
        this.currentUserContext = currentUserContext;
    }

    public async Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var currentUser = currentUserContext?.Current ?? CurrentUserInfo.Anonymous();
        var persistedEvent = new PersistedDomainEventEntity
        {
            Id = domainEvent.EventId,
            EventName = domainEvent.GetType().Name,
            OccurredAt = domainEvent.OccurredAt,
            Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            UserId = currentUser.UserId,
            UserName = currentUser.UserName,
            CorrelationId = currentUser.CorrelationId
        };

        await dbContext.DomainEvents.AddAsync(persistedEvent, cancellationToken);
    }
}
