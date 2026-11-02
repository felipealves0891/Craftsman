using System.Diagnostics.CodeAnalysis;
namespace Craftsman.App.Models;

[ExcludeFromCodeCoverage]
public sealed record FinancialSettlementSummaryViewModel(
    decimal TotalRevenueAmount,
    decimal TotalCostAmount,
    decimal TotalMarginAmount,
    decimal MarginPerOrderAmount,
    int OrderCount);
