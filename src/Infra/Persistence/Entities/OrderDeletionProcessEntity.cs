namespace Craftsman.Infra.Persistence.Entities;

public sealed class OrderDeletionProcessEntity : IPersistenceEntity<Guid>
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public int? RequestedByUserId { get; set; }

    public string? RequestedByUserName { get; set; }

    public DateTimeOffset RequestedAt { get; set; }

    public string Source { get; set; } = string.Empty;

    public string ExternalOrderId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateOnly? ShippingDate { get; set; }

    public int ItemCount { get; set; }

    public string ItemSummaryJson { get; set; } = "[]";

    public int ProductionTaskCount { get; set; }

    public int ShipmentCount { get; set; }

    public bool HadFinancialSettlement { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
