using Craftsman.Domain.Events;
using Craftsman.Domain.Finance.Entities;

namespace Craftsman.Tests.Finance;

public sealed class FinancialSettlementTests
{
    [Fact]
    public void Settlement_calculates_cost_margin_and_event_from_all_amounts()
    {
        var settlementId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var calculatedAt = DateTimeOffset.Parse("2026-10-15T12:00:00+00:00");

        var settlement = new FinancialSettlement(settlementId, orderId, 100, 37, 10, 5,
            calculatedAt: calculatedAt,
            laborCostAmount: 7);

        Assert.Equal(settlementId, settlement.Id);
        Assert.Equal(orderId, settlement.OrderId);
        Assert.Equal(7, settlement.LaborCostAmount);
        Assert.Equal(52, settlement.TotalCostAmount);
        Assert.Equal(48, settlement.MarginAmount);
        Assert.Equal(FinancialSettlementStatus.Calculated, settlement.Status);
        Assert.Equal(calculatedAt, settlement.CalculatedAt);
        var calculatedEvent = Assert.IsType<FinancialSettlementCalculatedEvent>(Assert.Single(settlement.DomainEvents));
        Assert.Equal(settlementId, calculatedEvent.SettlementId);
        Assert.Equal(orderId, calculatedEvent.OrderId);
        Assert.Equal(48, calculatedEvent.Margin);

        settlement.ClearDomainEvents();

        Assert.Empty(settlement.DomainEvents);
    }

    [Fact]
    public void Settlement_can_suppress_event_and_uses_current_time_by_default()
    {
        var before = DateTimeOffset.UtcNow;

        var settlement = new FinancialSettlement(Guid.NewGuid(), Guid.NewGuid(), 0, 0, 0,
            raiseCalculatedEvent: false);

        Assert.Empty(settlement.DomainEvents);
        Assert.InRange(settlement.CalculatedAt, before, DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Settlement_does_not_raise_calculated_event_for_non_calculated_status()
    {
        var settlement = new FinancialSettlement(Guid.NewGuid(), Guid.NewGuid(), 10, 3, 1,
            status: FinancialSettlementStatus.Failed);

        Assert.Equal(FinancialSettlementStatus.Failed, settlement.Status);
        Assert.Empty(settlement.DomainEvents);
    }

    [Fact]
    public void Settlement_rejects_empty_identifiers_and_negative_amounts()
    {
        var orderId = Guid.NewGuid();
        var settlementId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new FinancialSettlement(Guid.Empty, orderId, 0, 0, 0));
        Assert.Throws<ArgumentException>(() => new FinancialSettlement(settlementId, Guid.Empty, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FinancialSettlement(settlementId, orderId, -1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FinancialSettlement(settlementId, orderId, 0, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FinancialSettlement(settlementId, orderId, 0, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FinancialSettlement(settlementId, orderId, 0, 0, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FinancialSettlement(settlementId, orderId, 0, 0, 0, laborCostAmount: -1));
    }
}
