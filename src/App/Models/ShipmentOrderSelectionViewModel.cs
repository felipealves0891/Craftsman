namespace Craftsman.App.Models;

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
