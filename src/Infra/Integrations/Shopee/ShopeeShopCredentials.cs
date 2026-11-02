using System.Diagnostics.CodeAnalysis;
namespace Craftsman.Infra.Integrations.Shopee;

[ExcludeFromCodeCoverage]
public sealed record ShopeeShopCredentials(
    long ShopId,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    string AuthorizationStatus);
