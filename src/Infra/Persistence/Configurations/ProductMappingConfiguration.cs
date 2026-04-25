using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Craftsman.Infra.Persistence.Configurations;

public sealed class ProductMappingConfiguration : IEntityTypeConfiguration<ProductMappingEntity>
{
    public void Configure(EntityTypeBuilder<ProductMappingEntity> builder)
    {
        builder.ToTable("product_mappings");

        builder.HasKey(mapping => mapping.Id);

        builder.Property(mapping => mapping.Id).HasColumnName("id");
        builder.Property(mapping => mapping.Source).HasColumnName("source").HasMaxLength(100).IsRequired();
        builder.Property(mapping => mapping.ExternalItemId).HasColumnName("external_item_id").HasMaxLength(150).IsRequired();
        builder.Property(mapping => mapping.ProductId).HasColumnName("product_id");

        builder.HasIndex(mapping => new { mapping.Source, mapping.ExternalItemId }).IsUnique();
    }
}
