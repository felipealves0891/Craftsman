using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Craftsman.Infra.Persistence.Configurations;

public sealed class OrderDeletionProcessConfiguration : IEntityTypeConfiguration<OrderDeletionProcessEntity>
{
    public void Configure(EntityTypeBuilder<OrderDeletionProcessEntity> builder)
    {
        builder.ToTable("order_deletion_processes");

        builder.HasKey(process => process.Id);

        builder.Property(process => process.Id).HasColumnName("id");
        builder.Property(process => process.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(process => process.RequestedByUserId).HasColumnName("requested_by_user_id");
        builder.Property(process => process.RequestedByUserName).HasColumnName("requested_by_user_name").HasMaxLength(256);
        builder.Property(process => process.RequestedAt).HasColumnName("requested_at").IsRequired();
        builder.Property(process => process.Source).HasColumnName("source").HasMaxLength(100).IsRequired();
        builder.Property(process => process.ExternalOrderId).HasColumnName("external_order_id").HasMaxLength(150).IsRequired();
        builder.Property(process => process.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        builder.Property(process => process.ShippingDate).HasColumnName("shipping_date");
        builder.Property(process => process.ItemCount).HasColumnName("item_count").IsRequired();
        builder.Property(process => process.ItemSummaryJson).HasColumnName("item_summary_json").HasColumnType("jsonb").IsRequired();
        builder.Property(process => process.ProductionTaskCount).HasColumnName("production_task_count").IsRequired();
        builder.Property(process => process.ShipmentCount).HasColumnName("shipment_count").IsRequired();
        builder.Property(process => process.HadFinancialSettlement).HasColumnName("had_financial_settlement").IsRequired();
        builder.Property(process => process.CompletedAt).HasColumnName("completed_at");

        builder.HasIndex(process => process.OrderId);
        builder.HasIndex(process => process.RequestedAt);
    }
}
