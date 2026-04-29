using Craftsman.Domain.Shipping.Entities;

namespace Craftsman.Infra.Integrations.Loggi;

public static class LoggiTrackingStatusMapper
{
    private static readonly int[] CreatedStatuses = [1, 28];
    private static readonly int[] InTransitStatuses = [3, 4, 11, 13, 14, 15, 16, 17, 24, 25, 30];
    private static readonly int[] DeliveryAttemptedStatuses = [10, 12, 18, 20, 21, 22];
    private static readonly int[] NegativeExternalStatuses = [2, 6, 7, 8, 9, 19, 23, 26, 27, 29];

    public static ShipmentStatus ToShipmentStatus(LoggiTrackingEvent trackingEvent)
    {
        return trackingEvent.StatusCode switch
        {
            5 => ShipmentStatus.Delivered,
            var status when status is not null && CreatedStatuses.Contains(status.Value) => ShipmentStatus.Created,
            var status when status is not null && InTransitStatuses.Contains(status.Value) => ShipmentStatus.InTransit,
            var status when status is not null && DeliveryAttemptedStatuses.Contains(status.Value) => ShipmentStatus.DeliveryAttempted,
            var status when status is not null && NegativeExternalStatuses.Contains(status.Value) => ShipmentStatus.DeliveryAttempted,
            _ => ShipmentStatus.Created
        };
    }
}
