using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Craftsman.Infra.Persistence.Configurations;

public sealed class ShopeeShopConfiguration : IEntityTypeConfiguration<ShopeeShopEntity>
{
    public void Configure(EntityTypeBuilder<ShopeeShopEntity> builder)
    {
        builder.ToTable("shopee_shops");

        builder.HasKey(shop => shop.ShopId);

        builder.Property(shop => shop.ShopId).HasColumnName("shop_id");
        builder.Property(shop => shop.AccessToken).HasColumnName("access_token").HasMaxLength(4096).IsRequired();
        builder.Property(shop => shop.RefreshToken).HasColumnName("refresh_token").HasMaxLength(4096).IsRequired();
        builder.Property(shop => shop.AccessTokenExpiresAt).HasColumnName("access_token_expires_at").IsRequired();
        builder.Property(shop => shop.AuthorizationStatus).HasColumnName("authorization_status").HasMaxLength(50).IsRequired();
        builder.Property(shop => shop.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(shop => shop.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(shop => shop.AuthorizationStatus);
    }
}
