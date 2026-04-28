using Microsoft.Extensions.Options;

namespace Craftsman.Infra.Integrations.Shopee;

public sealed class ShopeeTokenService : IShopeeTokenService
{
    private readonly IShopeeClient client;
    private readonly IShopeeShopTokenRepository tokenRepository;
    private readonly ShopeeOptions options;

    public ShopeeTokenService(
        IShopeeClient client,
        IShopeeShopTokenRepository tokenRepository,
        IOptions<ShopeeOptions> options)
    {
        this.client = client;
        this.tokenRepository = tokenRepository;
        this.options = options.Value;
    }

    public async Task<ShopeeShopCredentials> AuthorizeAsync(long shopId, string code, CancellationToken cancellationToken = default)
    {
        var token = await client.ExchangeCodeAsync(shopId, code, cancellationToken);
        await tokenRepository.SaveTokensAsync(
            shopId,
            token.AccessToken,
            token.RefreshToken,
            DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn),
            cancellationToken);

        return await tokenRepository.GetByShopIdAsync(shopId, cancellationToken)
            ?? throw new InvalidOperationException("Shopee shop tokens were not saved.");
    }

    public async Task<ShopeeShopCredentials> EnsureValidAccessTokenAsync(ShopeeShopCredentials shop, CancellationToken cancellationToken = default)
    {
        var refreshThreshold = DateTimeOffset.UtcNow.AddMinutes(options.TokenRefreshSkewMinutes);
        if (shop.AccessTokenExpiresAt > refreshThreshold)
        {
            return shop;
        }

        try
        {
            var token = await client.RefreshAccessTokenAsync(shop.ShopId, shop.RefreshToken, cancellationToken);
            await tokenRepository.SaveTokensAsync(
                shop.ShopId,
                token.AccessToken,
                token.RefreshToken,
                DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn),
                cancellationToken);

            return await tokenRepository.GetByShopIdAsync(shop.ShopId, cancellationToken)
                ?? throw new InvalidOperationException("Shopee shop tokens were not saved.");
        }
        catch (ShopeeApiException exception) when (exception.Kind == ShopeeApiErrorKind.Authentication)
        {
            await tokenRepository.MarkReauthorizationRequiredAsync(shop.ShopId, cancellationToken);
            throw;
        }
    }
}
