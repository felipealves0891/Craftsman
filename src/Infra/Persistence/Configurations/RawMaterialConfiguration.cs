using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Craftsman.Infra.Persistence.Configurations;

public sealed class RawMaterialConfiguration : IEntityTypeConfiguration<RawMaterialEntity>
{
    public void Configure(EntityTypeBuilder<RawMaterialEntity> builder)
    {
        builder.ToTable("raw_materials");

        builder.HasKey(material => material.Id);

        builder.Property(material => material.Id).HasColumnName("id");
        builder.Property(material => material.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(material => material.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(50).IsRequired();
        builder.Property(material => material.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        builder.Property(material => material.MinimumStockLevel).HasColumnName("minimum_stock_level").HasPrecision(18, 2);
        builder.Property(material => material.CriticalStockLevel).HasColumnName("critical_stock_level").HasPrecision(18, 2);
    }
}
