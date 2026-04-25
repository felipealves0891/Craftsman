using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Craftsman.Infra.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<OrderEntity>
{
    public void Configure(EntityTypeBuilder<OrderEntity> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(order => order.Id);

        builder.Property(order => order.Id).HasColumnName("id");
        builder.Property(order => order.Source).HasColumnName("source").HasMaxLength(100).IsRequired();
        builder.Property(order => order.ExternalOrderId).HasColumnName("external_order_id").HasMaxLength(150).IsRequired();
        builder.Property(order => order.CustomerName).HasColumnName("customer_name").HasMaxLength(200).IsRequired();
        builder.Property(order => order.CustomerEmail).HasColumnName("customer_email").HasMaxLength(320);
        builder.Property(order => order.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        builder.Property(order => order.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(order => new { order.Source, order.ExternalOrderId }).IsUnique();

        builder.HasMany(order => order.Items)
            .WithOne(item => item.Order)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
