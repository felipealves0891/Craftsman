namespace Craftsman.App.Models;

public sealed record FinancialSettlementSummaryViewModel(
    decimal TotalRevenueAmount,
    decimal TotalCostAmount,
    decimal TotalMarginAmount,
    decimal MarginPerOrderAmount,
    int OrderCount);
