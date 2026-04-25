using Craftsman.Domain.Events;
using Craftsman.Domain.Shipping.Entities;

namespace Craftsman.Tests.Shipping;

public sealed class ShipmentTests
{
    [Fact]
    public void Shipment_records_traceability_and_created_event()
    {
        var orderId = Guid.NewGuid();

        var shipment = new Shipment(Guid.NewGuid(), orderId, "TRACK-1");

        Assert.Equal(orderId, shipment.OrderId);
        Assert.Equal("TRACK-1", shipment.TrackingCode);
        Assert.Equal(ShipmentStatus.Created, shipment.Status);
        Assert.Contains(shipment.DomainEvents, domainEvent => domainEvent is ShipmentCreatedEvent);
    }

    [Fact]
    public void Invalid_status_transition_is_blocked()
    {
        var shipment = new Shipment(Guid.NewGuid(), Guid.NewGuid(), "TRACK-1");

        var exception = Assert.Throws<InvalidOperationException>(shipment.MarkDeliveryAttempted);

        Assert.Contains("expected InTransit", exception.Message);
    }
}
