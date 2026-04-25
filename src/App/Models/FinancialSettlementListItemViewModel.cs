namespace Craftsman.App.Models;

public sealed record FinancialSettlementListItemViewModel(
    Guid Id,
    Guid OrderId,
    decimal RevenueAmount,
    decimal ProductionCostAmount,
    decimal ShippingCostAmount,
    decimal TotalCostAmount,
    decimal MarginAmount,
    string Status,
    DateTimeOffset CalculatedAt);
