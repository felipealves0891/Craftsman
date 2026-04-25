using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Craftsman.Infra.Persistence.Configurations;

public sealed class ProductionTaskConfiguration : IEntityTypeConfiguration<ProductionTaskEntity>
{
    public void Configure(EntityTypeBuilder<ProductionTaskEntity> builder)
    {
        builder.ToTable("production_tasks");

        builder.HasKey(task => task.Id);

        builder.Property(task => task.Id).HasColumnName("id");
        builder.Property(task => task.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(task => task.OrderItemId).HasColumnName("order_item_id").IsRequired();
        builder.Property(task => task.ProductId).HasColumnName("product_id").IsRequired();
        builder.Property(task => task.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(task => task.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        builder.Property(task => task.PlannedAt).HasColumnName("planned_at").IsRequired();
        builder.Property(task => task.StartedAt).HasColumnName("started_at");
        builder.Property(task => task.CompletedAt).HasColumnName("completed_at");

        builder.HasIndex(task => task.OrderId);
        builder.HasIndex(task => task.Status);
    }
}
