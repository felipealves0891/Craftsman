using System.Text.Json;

namespace Craftsman.Domain.Sales.Entities;

public sealed class OrderDeletionProcess
{
    public OrderDeletionProcess(
        Guid id,
        Guid orderId,
        int? requestedByUserId,
        string? requestedByUserName,
        DateTimeOffset requestedAt,
        string source,
        string externalOrderId,
        string status,
        DateOnly? shippingDate,
        int itemCount,
        string itemSummaryJson,
        int productionTaskCount,
        int shipmentCount,
        bool hadFinancialSettlement,
        DateTimeOffset? completedAt = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Deletion process id is required.", nameof(id));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order id is required.", nameof(orderId));
        }

        Id = id;
        OrderId = orderId;
        RequestedByUserId = requestedByUserId;
        RequestedByUserName = requestedByUserName;
        RequestedAt = requestedAt;
        Source = source;
        ExternalOrderId = externalOrderId;
        Status = status;
        ShippingDate = shippingDate;
        ItemCount = itemCount;
        ItemSummaryJson = itemSummaryJson;
        ProductionTaskCount = productionTaskCount;
        ShipmentCount = shipmentCount;
        HadFinancialSettlement = hadFinancialSettlement;
        CompletedAt = completedAt;
    }

    public Guid Id { get; }

    public Guid OrderId { get; }

    public int? RequestedByUserId { get; }

    public string? RequestedByUserName { get; }

    public DateTimeOffset RequestedAt { get; }

    public string Source { get; }

    public string ExternalOrderId { get; }

    public string Status { get; }

    public DateOnly? ShippingDate { get; }

    public int ItemCount { get; }

    public string ItemSummaryJson { get; }

    public int ProductionTaskCount { get; }

    public int ShipmentCount { get; }

    public bool HadFinancialSettlement { get; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public bool IsCompleted => CompletedAt.HasValue;

    public void MarkCompleted(DateTimeOffset completedAt)
    {
        CompletedAt ??= completedAt;
    }

    public static string SerializeItemSummary(IEnumerable<OrderDeletionProcessItemSummary> items)
    {
        return JsonSerializer.Serialize(items);
    }
}

public sealed record OrderDeletionProcessItemSummary(
    Guid ItemId,
    string ExternalItemId,
    string Description,
    int Quantity,
    Guid? ProductId);
