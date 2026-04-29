namespace Craftsman.Infra.Integrations.Loggi;

public interface ILoggiTokenService
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
