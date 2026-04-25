using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Craftsman.Infra.Persistence.Configurations;

public sealed class BillOfMaterialsItemConfiguration : IEntityTypeConfiguration<BillOfMaterialsItemEntity>
{
    public void Configure(EntityTypeBuilder<BillOfMaterialsItemEntity> builder)
    {
        builder.ToTable("bill_of_materials_items");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.ProductId).HasColumnName("product_id");
        builder.Property(item => item.RawMaterialId).HasColumnName("raw_material_id").IsRequired();
        builder.Property(item => item.QuantityPerUnit).HasColumnName("quantity_per_unit").HasPrecision(18, 4).IsRequired();

        builder.HasIndex(item => new { item.ProductId, item.RawMaterialId }).IsUnique();
    }
}
