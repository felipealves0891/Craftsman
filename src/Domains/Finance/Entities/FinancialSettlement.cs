using Craftsman.Domain.Events;

namespace Craftsman.Domain.Finance.Entities;

public sealed class FinancialSettlement
{
    private readonly List<IDomainEvent> domainEvents = [];

    public Guid Id { get; }

    public Guid OrderId { get; }

    public decimal RevenueAmount { get; }

    public decimal ProductionCostAmount { get; private set; }

    public decimal LaborCostAmount { get; private set; }

    public bool LaborCostReconciled { get; private set; }

    public decimal ShippingCostAmount { get; }

    public decimal DiscountAmount { get; }

    public decimal TotalCostAmount => ProductionCostAmount + ShippingCostAmount + DiscountAmount;

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
        decimal discountAmount = 0,
        FinancialSettlementStatus status = FinancialSettlementStatus.Calculated,
        DateTimeOffset? calculatedAt = null,
        bool raiseCalculatedEvent = true,
        decimal laborCostAmount = 0,
        bool laborCostReconciled = true)
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

        if (laborCostAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(laborCostAmount), "Labor cost cannot be negative.");
        }

        if (discountAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(discountAmount), "Discount cannot be negative.");
        }

        Id = id;
        OrderId = orderId;
        RevenueAmount = revenueAmount;
        ProductionCostAmount = productionCostAmount;
        LaborCostAmount = laborCostAmount;
        LaborCostReconciled = laborCostReconciled;
        ShippingCostAmount = shippingCostAmount;
        DiscountAmount = discountAmount;
        Status = status;
        CalculatedAt = calculatedAt ?? DateTimeOffset.UtcNow;

        if (raiseCalculatedEvent && Status == FinancialSettlementStatus.Calculated)
        {
            domainEvents.Add(new FinancialSettlementCalculatedEvent(Id, OrderId, MarginAmount));
        }
    }

    public void ReconcileLaborCost(decimal laborCostAmount, decimal productionCostAmount)
    {
        if (LaborCostReconciled)
        {
            throw new InvalidOperationException("Labor cost has already been reconciled.");
        }

        if (laborCostAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(laborCostAmount), "Labor cost cannot be negative.");
        }

        if (productionCostAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productionCostAmount), "Production cost cannot be negative.");
        }

        LaborCostAmount = laborCostAmount;
        ProductionCostAmount = productionCostAmount;
        LaborCostReconciled = true;
    }

    public void ClearDomainEvents()
    {
        domainEvents.Clear();
    }
}
