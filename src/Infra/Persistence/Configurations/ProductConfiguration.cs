using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Craftsman.Infra.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<ProductEntity>
{
    public void Configure(EntityTypeBuilder<ProductEntity> builder)
    {
        builder.ToTable("products");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Id).HasColumnName("id");
        builder.Property(product => product.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(product => product.Status).HasColumnName("status").HasMaxLength(50).IsRequired();

        builder.HasMany(product => product.BillOfMaterials)
            .WithOne(item => item.Product)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(product => product.Mappings)
            .WithOne(mapping => mapping.Product)
            .HasForeignKey(mapping => mapping.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
