namespace Craftsman.App.Models;

public sealed record ShipmentListItemViewModel(
    Guid Id,
    Guid OrderId,
    string? TrackingCode,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ShippedAt,
    DateTimeOffset? DeliveredAt);
