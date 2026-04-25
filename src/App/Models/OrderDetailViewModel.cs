namespace Craftsman.App.Models;

public sealed record OrderDetailViewModel(
    Guid Id,
    string Source,
    string ExternalOrderId,
    string CustomerName,
    string? CustomerEmail,
    string Status,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<OrderItemViewModel> Items,
    IReadOnlyCollection<ProductionTaskListItemViewModel> ProductionTasks,
    IReadOnlyCollection<ShipmentListItemViewModel> Shipments);
