using System.Diagnostics.CodeAnalysis;
namespace Craftsman.App.Models;

[ExcludeFromCodeCoverage]
public sealed record FinancialSettlementIndexViewModel(
    IReadOnlyCollection<FinancialSettlementListItemViewModel> Settlements,
    FinancialSettlementSummaryViewModel Summary);
