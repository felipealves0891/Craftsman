namespace Craftsman.Infra.Persistence.Entities;

public sealed class PersistedDomainEventEntity : IPersistenceEntity<Guid>
{
    public Guid Id { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }
}
