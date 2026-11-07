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
    IReadOnlyCollection<ShipmentListItemViewModel> Shipments)
{
    public bool CanRevertProduction =>
        Status is nameof(Craftsman.Domain.Sales.Entities.OrderStatus.ReadyForProduction) or nameof(Craftsman.Domain.Sales.Entities.OrderStatus.InProduction)
        && ProductionTasks.Count > 0
        && !ProductionTasks.Any(task => task.Status == nameof(Craftsman.Domain.Production.Entities.ProductionTaskStatus.Completed))
        && !Shipments.Any();
}
