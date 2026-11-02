using System.Diagnostics.CodeAnalysis;
namespace Craftsman.App.Models;

[ExcludeFromCodeCoverage]
public sealed record FinancialSettlementPendingViewModel(
    Guid OrderId,
    string Source,
    string ExternalOrderId,
    DateTimeOffset? DeliveredAt);
