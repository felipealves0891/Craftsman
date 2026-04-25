namespace Craftsman.Domain.Events;

public sealed record ProductionPlannedEvent(Guid ProductionTaskId, Guid OrderId) : DomainEvent;
