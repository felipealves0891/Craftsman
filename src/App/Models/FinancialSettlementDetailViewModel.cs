using System.Diagnostics.CodeAnalysis;
namespace Craftsman.App.Models;

[ExcludeFromCodeCoverage]
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
