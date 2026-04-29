namespace Craftsman.Infra.Integrations.Loggi;

public interface ILoggiTrackingClient
{
    Task<LoggiTrackingEvent?> GetLatestEventAsync(string trackingCode, CancellationToken cancellationToken = default);
}
