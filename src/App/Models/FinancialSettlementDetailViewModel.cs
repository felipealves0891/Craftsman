namespace Craftsman.App.Models;

public sealed record FinancialSettlementDetailViewModel(
    Guid Id,
    Guid OrderId,
    decimal RevenueAmount,
    decimal ProductionCostAmount,
    decimal ShippingCostAmount,
    decimal DiscountAmount,
    decimal TotalCostAmount,
    decimal MarginAmount,
    string Status,
    DateTimeOffset CalculatedAt,
    IReadOnlyCollection<OrderItemViewModel> Items);
