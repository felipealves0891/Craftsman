using System.Diagnostics.CodeAnalysis;
namespace Craftsman.App.Models;

[ExcludeFromCodeCoverage]
public sealed record OrderDetailViewModel(
    Guid Id,
    string Source,
    string ExternalOrderId,
    string? CustomerName,
    string? CustomerPhone,
    string? CustomerEmail,
    string? DiscountDescription,
    decimal DiscountAmount,
    string Status,
    DateTimeOffset CreatedAt,
    DateOnly? ShippingDate,
    IReadOnlyCollection<OrderItemViewModel> Items,
    IReadOnlyCollection<ProductionTaskListItemViewModel> ProductionTasks,
    IReadOnlyCollection<ShipmentListItemViewModel> Shipments);
