namespace Craftsman.Infra.Persistence.Entities;

public sealed class FinancialSettlementEntity : IPersistenceEntity<Guid>
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public decimal RevenueAmount { get; set; }

    public decimal ProductionCostAmount { get; set; }

    public decimal ShippingCostAmount { get; set; }

    public decimal TotalCostAmount { get; set; }

    public decimal MarginAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CalculatedAt { get; set; }
}
