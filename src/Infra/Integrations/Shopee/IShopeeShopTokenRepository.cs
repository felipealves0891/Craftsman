namespace Craftsman.Infra.Integrations.Shopee;

public interface IShopeeShopTokenRepository
{
    Task<IReadOnlyCollection<ShopeeShopCredentials>> ListAuthorizedAsync(CancellationToken cancellationToken = default);

    Task<ShopeeShopCredentials?> GetByShopIdAsync(long shopId, CancellationToken cancellationToken = default);

    Task SaveTokensAsync(
        long shopId,
        string accessToken,
        string refreshToken,
        DateTimeOffset accessTokenExpiresAt,
        CancellationToken cancellationToken = default);

    Task MarkReauthorizationRequiredAsync(long shopId, CancellationToken cancellationToken = default);
}
