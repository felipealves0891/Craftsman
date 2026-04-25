using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Craftsman.Infra.Persistence.Configurations;

public sealed class FinancialSettlementConfiguration : IEntityTypeConfiguration<FinancialSettlementEntity>
{
    public void Configure(EntityTypeBuilder<FinancialSettlementEntity> builder)
    {
        builder.ToTable("financial_settlements");

        builder.HasKey(settlement => settlement.Id);

        builder.Property(settlement => settlement.Id).HasColumnName("id");
        builder.Property(settlement => settlement.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(settlement => settlement.RevenueAmount).HasColumnName("revenue_amount").HasPrecision(18, 2).IsRequired();
        builder.Property(settlement => settlement.ProductionCostAmount).HasColumnName("production_cost_amount").HasPrecision(18, 2).IsRequired();
        builder.Property(settlement => settlement.ShippingCostAmount).HasColumnName("shipping_cost_amount").HasPrecision(18, 2).IsRequired();
        builder.Property(settlement => settlement.TotalCostAmount).HasColumnName("total_cost_amount").HasPrecision(18, 2).IsRequired();
        builder.Property(settlement => settlement.MarginAmount).HasColumnName("margin_amount").HasPrecision(18, 2).IsRequired();
        builder.Property(settlement => settlement.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        builder.Property(settlement => settlement.CalculatedAt).HasColumnName("calculated_at").IsRequired();

        builder.HasIndex(settlement => settlement.OrderId).IsUnique();
        builder.HasIndex(settlement => settlement.CalculatedAt);
    }
}
