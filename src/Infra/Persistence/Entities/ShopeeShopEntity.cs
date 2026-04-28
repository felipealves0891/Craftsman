namespace Craftsman.Infra.Persistence.Entities;

public sealed class ShopeeShopEntity : IPersistenceEntity<long>
{
    public long Id
    {
        get => ShopId;
        set => ShopId = value;
    }

    public long ShopId { get; set; }

    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public DateTimeOffset AccessTokenExpiresAt { get; set; }

    public string AuthorizationStatus { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
