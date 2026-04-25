namespace Craftsman.Infra.Persistence.Entities;

public sealed class ShipmentEntity : IPersistenceEntity<Guid>
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string? TrackingCode { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ShippedAt { get; set; }

    public DateTimeOffset? DeliveredAt { get; set; }
}
