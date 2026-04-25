using Craftsman.Domain.Events;

namespace Craftsman.Domain.Finance.Entities;

public sealed class FinancialSettlement
{
    private readonly List<IDomainEvent> domainEvents = [];

    public Guid Id { get; }

    public Guid OrderId { get; }

    public decimal RevenueAmount { get; }

    public decimal ProductionCostAmount { get; }

    public decimal ShippingCostAmount { get; }

    public decimal TotalCostAmount => ProductionCostAmount + ShippingCostAmount;

    public decimal MarginAmount => RevenueAmount - TotalCostAmount;

    public FinancialSettlementStatus Status { get; }

    public DateTimeOffset CalculatedAt { get; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => domainEvents.AsReadOnly();

    public FinancialSettlement(
        Guid id,
        Guid orderId,
        decimal revenueAmount,
        decimal productionCostAmount,
        decimal shippingCostAmount,
        FinancialSettlementStatus status = FinancialSettlementStatus.Calculated,
        DateTimeOffset? calculatedAt = null,
        bool raiseCalculatedEvent = true)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Settlement id is required.", nameof(id));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order id is required.", nameof(orderId));
        }

        if (revenueAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(revenueAmount), "Revenue cannot be negative.");
        }

        if (productionCostAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productionCostAmount), "Production cost cannot be negative.");
        }

        if (shippingCostAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shippingCostAmount), "Shipping cost cannot be negative.");
        }

        Id = id;
        OrderId = orderId;
        RevenueAmount = revenueAmount;
        ProductionCostAmount = productionCostAmount;
        ShippingCostAmount = shippingCostAmount;
        Status = status;
        CalculatedAt = calculatedAt ?? DateTimeOffset.UtcNow;

        if (raiseCalculatedEvent && Status == FinancialSettlementStatus.Calculated)
        {
            domainEvents.Add(new FinancialSettlementCalculatedEvent(Id, OrderId, MarginAmount));
        }
    }

    public void ClearDomainEvents()
    {
        domainEvents.Clear();
    }
}
