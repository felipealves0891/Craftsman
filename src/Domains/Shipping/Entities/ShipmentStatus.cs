namespace Craftsman.Domain.Shipping.Entities;

public enum ShipmentStatus
{
    Created = 1,
    InTransit = 2,
    DeliveryAttempted = 3,
    Delivered = 4,
    Cancelled = 5
}
