namespace Craftsman.Infra.Persistence.Entities;

public sealed class DomainEventHandlerExecutionEntity : IPersistenceEntity<Guid>
{
    public Guid Id { get; set; }

    public Guid DomainEventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string HandlerName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int AttemptCount { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public PersistedDomainEventEntity? DomainEvent { get; set; }
}
