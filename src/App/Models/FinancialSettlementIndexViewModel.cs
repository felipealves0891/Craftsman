namespace Craftsman.App.Models;

public sealed record FinancialSettlementIndexViewModel(
    IReadOnlyCollection<FinancialSettlementListItemViewModel> Settlements,
    FinancialSettlementSummaryViewModel Summary);
