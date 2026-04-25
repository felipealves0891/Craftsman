namespace Craftsman.Domain.Events;

public sealed record ShipmentCreatedEvent(Guid ShipmentId, Guid OrderId) : DomainEvent;
