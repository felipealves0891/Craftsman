using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Craftsman.Infra.Persistence.Configurations;

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovementEntity>
{
    public void Configure(EntityTypeBuilder<StockMovementEntity> builder)
    {
        builder.ToTable("stock_movements");

        builder.HasKey(movement => movement.Id);

        builder.Property(movement => movement.Id).HasColumnName("id");
        builder.Property(movement => movement.RawMaterialId).HasColumnName("raw_material_id");
        builder.Property(movement => movement.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
        builder.Property(movement => movement.Quantity).HasColumnName("quantity").HasPrecision(18, 4).IsRequired();
        builder.Property(movement => movement.Reason).HasColumnName("reason").HasMaxLength(300).IsRequired();
        builder.Property(movement => movement.BusinessReference).HasColumnName("business_reference").HasMaxLength(150);
        builder.Property(movement => movement.OccurredAt).HasColumnName("occurred_at").IsRequired();

        builder.HasOne(movement => movement.RawMaterial)
            .WithMany()
            .HasForeignKey(movement => movement.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(movement => movement.RawMaterialId);
    }
}
