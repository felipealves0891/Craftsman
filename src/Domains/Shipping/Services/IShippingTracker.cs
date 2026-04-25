namespace Craftsman.Domain.Shipping.Services;

public interface IShippingTracker
{
    Task<TrackedShipmentStatus?> TrackAsync(string trackingCode, CancellationToken cancellationToken = default);
}
