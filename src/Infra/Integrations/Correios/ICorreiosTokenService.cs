namespace Craftsman.Infra.Integrations.Correios;

public interface ICorreiosTokenService
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
