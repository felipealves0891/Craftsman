namespace Craftsman.Domain.Events;

public abstract record DomainEvent(Guid EventId, DateTimeOffset OccurredAt) : IDomainEvent
{
    protected DomainEvent()
        : this(Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}
