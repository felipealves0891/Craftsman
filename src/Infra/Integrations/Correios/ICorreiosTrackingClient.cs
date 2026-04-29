namespace Craftsman.Infra.Integrations.Correios;

public interface ICorreiosTrackingClient
{
    Task<CorreiosTrackingEvent?> GetLatestEventAsync(string trackingCode, CancellationToken cancellationToken = default);
}
