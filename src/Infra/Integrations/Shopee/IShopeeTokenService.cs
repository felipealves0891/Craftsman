namespace Craftsman.Infra.Integrations.Shopee;

public interface IShopeeTokenService
{
    Task<ShopeeShopCredentials> AuthorizeAsync(long shopId, string code, CancellationToken cancellationToken = default);

    Task<ShopeeShopCredentials> EnsureValidAccessTokenAsync(ShopeeShopCredentials shop, CancellationToken cancellationToken = default);
}
