namespace Craftsman.Domain.Events;

public sealed record DeliveryConfirmedEvent(Guid ShipmentId, Guid OrderId, DateTimeOffset DeliveredAt) : DomainEvent;
