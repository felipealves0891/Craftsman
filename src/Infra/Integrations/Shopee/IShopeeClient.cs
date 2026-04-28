namespace Craftsman.Infra.Integrations.Shopee;

public interface IShopeeClient
{
    Task<ShopeeTokenResponse> ExchangeCodeAsync(long shopId, string code, CancellationToken cancellationToken = default);

    Task<ShopeeTokenResponse> RefreshAccessTokenAsync(long shopId, string refreshToken, CancellationToken cancellationToken = default);

    Task<ShopeeOrderListResponse> GetOrderListAsync(
        ShopeeShopCredentials shop,
        DateTimeOffset from,
        DateTimeOffset to,
        string status,
        string? cursor,
        CancellationToken cancellationToken = default);

    Task<ShopeeOrderDetail?> GetOrderDetailAsync(
        ShopeeShopCredentials shop,
        string orderSn,
        CancellationToken cancellationToken = default);
}
