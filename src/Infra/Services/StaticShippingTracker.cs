using Craftsman.Domain.Shipping.Entities;
using Craftsman.Domain.Shipping.Services;

namespace Craftsman.Infra.Services;

public sealed class StaticShippingTracker : IShippingTracker
{
    public Task<TrackedShipmentStatus?> TrackAsync(string trackingCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(trackingCode))
        {
            return Task.FromResult<TrackedShipmentStatus?>(null);
        }

        return Task.FromResult<TrackedShipmentStatus?>(new TrackedShipmentStatus(
            trackingCode.Trim(),
            ShipmentStatus.InTransit,
            DateTimeOffset.UtcNow));
    }
}
