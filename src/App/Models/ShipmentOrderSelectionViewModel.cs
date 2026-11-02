using System.Diagnostics.CodeAnalysis;
namespace Craftsman.App.Models;

[ExcludeFromCodeCoverage]
public sealed record ShipmentOrderSelectionViewModel(
    Guid Id,
    string Source,
    string ExternalOrderId,
    string Status,
    DateOnly? ShippingDate,
    IReadOnlyCollection<OrderItemViewModel> Items)
{
    public string DisplayName => $"{Source} - {ExternalOrderId}";
}
