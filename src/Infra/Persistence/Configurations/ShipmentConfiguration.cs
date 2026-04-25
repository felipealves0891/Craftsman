using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Craftsman.Infra.Persistence.Configurations;

public sealed class ShipmentConfiguration : IEntityTypeConfiguration<ShipmentEntity>
{
    public void Configure(EntityTypeBuilder<ShipmentEntity> builder)
    {
        builder.ToTable("shipments");

        builder.HasKey(shipment => shipment.Id);

        builder.Property(shipment => shipment.Id).HasColumnName("id");
        builder.Property(shipment => shipment.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(shipment => shipment.TrackingCode).HasColumnName("tracking_code").HasMaxLength(150);
        builder.Property(shipment => shipment.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        builder.Property(shipment => shipment.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(shipment => shipment.ShippedAt).HasColumnName("shipped_at");
        builder.Property(shipment => shipment.DeliveredAt).HasColumnName("delivered_at");

        builder.HasIndex(shipment => shipment.OrderId);
        builder.HasIndex(shipment => shipment.Status);
    }
}
