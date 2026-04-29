using Craftsman.Domain.Shipping.Services;

namespace Craftsman.Infra.Integrations.Correios;

public sealed class CorreiosShippingTracker : IShippingTracker
{
    private readonly ICorreiosTrackingClient trackingClient;

    public CorreiosShippingTracker(ICorreiosTrackingClient trackingClient)
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
            CorreiosTrackingStatusMapper.ToShipmentStatus(latestEvent),
            latestEvent.CreatedAt ?? DateTimeOffset.UtcNow);
    }
}
