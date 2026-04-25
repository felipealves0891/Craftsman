using Craftsman.Domain.Events;

namespace Craftsman.Domain.Shipping.Entities;

public sealed class Shipment
{
    private readonly List<IDomainEvent> domainEvents = [];

    public Guid Id { get; }

    public Guid OrderId { get; }

    public string? TrackingCode { get; private set; }

    public ShipmentStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? ShippedAt { get; private set; }

    public DateTimeOffset? DeliveredAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => domainEvents.AsReadOnly();

    public Shipment(
        Guid id,
        Guid orderId,
        string? trackingCode = null,
        ShipmentStatus status = ShipmentStatus.Created,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? shippedAt = null,
        DateTimeOffset? deliveredAt = null,
        bool raiseCreatedEvent = true)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Shipment id is required.", nameof(id));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order id is required.", nameof(orderId));
        }

        Id = id;
        OrderId = orderId;
        TrackingCode = string.IsNullOrWhiteSpace(trackingCode) ? null : trackingCode.Trim();
        Status = status;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        ShippedAt = shippedAt;
        DeliveredAt = deliveredAt;

        if (raiseCreatedEvent)
        {
            domainEvents.Add(new ShipmentCreatedEvent(Id, OrderId));
        }
    }

    public void UpdateTrackingCode(string? trackingCode)
    {
        TrackingCode = string.IsNullOrWhiteSpace(trackingCode) ? null : trackingCode.Trim();
    }

    public void MarkInTransit()
    {
        EnsureStatus(ShipmentStatus.Created);
        Status = ShipmentStatus.InTransit;
        ShippedAt = DateTimeOffset.UtcNow;
    }

    public void MarkDeliveryAttempted()
    {
        EnsureStatus(ShipmentStatus.InTransit);
        Status = ShipmentStatus.DeliveryAttempted;
    }

    public void MarkDelivered()
    {
        if (Status is not ShipmentStatus.InTransit and not ShipmentStatus.DeliveryAttempted)
        {
            throw new InvalidOperationException($"Shipment cannot be delivered from status {Status}.");
        }

        Status = ShipmentStatus.Delivered;
        DeliveredAt = DateTimeOffset.UtcNow;
        domainEvents.Add(new DeliveryConfirmedEvent(Id, OrderId, DeliveredAt.Value));
    }

    public void Cancel()
    {
        if (Status is ShipmentStatus.Delivered or ShipmentStatus.Cancelled)
        {
            throw new InvalidOperationException($"Shipment cannot be cancelled from status {Status}.");
        }

        Status = ShipmentStatus.Cancelled;
    }

    public void ClearDomainEvents()
    {
        domainEvents.Clear();
    }

    private void EnsureStatus(ShipmentStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"Shipment cannot move from {Status}; expected {expected}.");
        }
    }
}
