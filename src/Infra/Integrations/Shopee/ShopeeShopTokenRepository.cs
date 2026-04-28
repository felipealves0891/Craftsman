using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Infra.Integrations.Shopee;

public sealed class ShopeeShopTokenRepository : IShopeeShopTokenRepository
{
    private readonly AppDbContext dbContext;
    private readonly IDataProtector protector;

    public ShopeeShopTokenRepository(AppDbContext dbContext, IDataProtectionProvider dataProtectionProvider)
    {
        this.dbContext = dbContext;
        protector = dataProtectionProvider.CreateProtector("Craftsman.Shopee.ShopTokens.v1");
    }

    public async Task<IReadOnlyCollection<ShopeeShopCredentials>> ListAuthorizedAsync(CancellationToken cancellationToken = default)
    {
        var shops = await dbContext.ShopeeShops
            .AsNoTracking()
            .Where(shop => shop.AuthorizationStatus == ShopeeShopAuthorizationStatus.Active)
            .ToListAsync(cancellationToken);

        return shops.Select(ToCredentials).ToList().AsReadOnly();
    }

    public async Task<ShopeeShopCredentials?> GetByShopIdAsync(long shopId, CancellationToken cancellationToken = default)
    {
        var shop = await dbContext.ShopeeShops
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.ShopId == shopId, cancellationToken);

        return shop is null ? null : ToCredentials(shop);
    }

    public async Task SaveTokensAsync(
        long shopId,
        string accessToken,
        string refreshToken,
        DateTimeOffset accessTokenExpiresAt,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var shop = await dbContext.ShopeeShops.FirstOrDefaultAsync(entity => entity.ShopId == shopId, cancellationToken);

        if (shop is null)
        {
            shop = new ShopeeShopEntity
            {
                ShopId = shopId,
                CreatedAt = now
            };
            await dbContext.ShopeeShops.AddAsync(shop, cancellationToken);
        }

        shop.AccessToken = protector.Protect(accessToken);
        shop.RefreshToken = protector.Protect(refreshToken);
        shop.AccessTokenExpiresAt = accessTokenExpiresAt;
        shop.AuthorizationStatus = ShopeeShopAuthorizationStatus.Active;
        shop.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkReauthorizationRequiredAsync(long shopId, CancellationToken cancellationToken = default)
    {
        var shop = await dbContext.ShopeeShops.FirstOrDefaultAsync(entity => entity.ShopId == shopId, cancellationToken);
        if (shop is null)
        {
            return;
        }

        shop.AuthorizationStatus = ShopeeShopAuthorizationStatus.ReauthorizationRequired;
        shop.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private ShopeeShopCredentials ToCredentials(ShopeeShopEntity shop)
    {
        return new ShopeeShopCredentials(
            shop.ShopId,
            protector.Unprotect(shop.AccessToken),
            protector.Unprotect(shop.RefreshToken),
            shop.AccessTokenExpiresAt,
            shop.AuthorizationStatus);
    }
}
