using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Craftsman.Infra.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItemEntity>
{
    public void Configure(EntityTypeBuilder<OrderItemEntity> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.OrderId).HasColumnName("order_id");
        builder.Property(item => item.ExternalItemId).HasColumnName("external_item_id").HasMaxLength(150).IsRequired();
        builder.Property(item => item.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
        builder.Property(item => item.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(item => item.UnitPriceAmount).HasColumnName("unit_price_amount").HasPrecision(18, 2).IsRequired();
        builder.Property(item => item.UnitPriceCurrency).HasColumnName("unit_price_currency").HasMaxLength(3).IsRequired();
        builder.Property(item => item.ProductId).HasColumnName("product_id");
    }
}
