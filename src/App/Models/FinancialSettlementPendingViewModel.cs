namespace Craftsman.App.Models;

public sealed record FinancialSettlementPendingViewModel(
    Guid OrderId,
    string Source,
    string ExternalOrderId,
    DateTimeOffset? DeliveredAt);
