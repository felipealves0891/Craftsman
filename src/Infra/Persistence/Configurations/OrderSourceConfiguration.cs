using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Craftsman.Infra.Persistence.Configurations;

public sealed class OrderSourceConfiguration : IEntityTypeConfiguration<OrderSourceEntity>
{
    public void Configure(EntityTypeBuilder<OrderSourceEntity> builder)
    {
        builder.ToTable("order_sources");

        builder.HasKey(source => source.Id);

        builder.Property(source => source.Id).HasColumnName("id");
        builder.Property(source => source.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(source => source.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(source => source.Name).IsUnique();
    }
}
