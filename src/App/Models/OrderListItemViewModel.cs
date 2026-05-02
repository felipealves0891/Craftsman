namespace Craftsman.App.Models;

public sealed record OrderListItemViewModel(
    Guid Id,
    string Source,
    string ExternalOrderId,
    string Status,
    int ItemCount,
    DateTimeOffset CreatedAt,
    DateOnly? ShippingDate);
