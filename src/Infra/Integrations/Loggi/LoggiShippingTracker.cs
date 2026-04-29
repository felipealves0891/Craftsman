using Craftsman.Domain.Shipping.Services;

namespace Craftsman.Infra.Integrations.Loggi;

public sealed class LoggiShippingTracker : IShippingTracker
{
    private readonly ILoggiTrackingClient trackingClient;

    public LoggiShippingTracker(ILoggiTrackingClient trackingClient)
    {
        this.trackingClient = trackingClient;
    }

    public async Task<TrackedShipmentStatus?> TrackAsync(string trackingCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(trackingCode))
        {
            return null;
        }

        var latestEvent = await trackingClient.GetLatestEventAsync(trackingCode.Trim(), cancellationToken);
        if (latestEvent is null)
        {
            return null;
        }

        return new TrackedShipmentStatus(
            latestEvent.TrackingCode,
            LoggiTrackingStatusMapper.ToShipmentStatus(latestEvent),
            latestEvent.UpdatedAt ?? DateTimeOffset.UtcNow);
    }
}
