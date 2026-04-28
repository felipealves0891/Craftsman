namespace Craftsman.Infra.Integrations.Shopee;

public sealed record ShopeeShopCredentials(
    long ShopId,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    string AuthorizationStatus);
