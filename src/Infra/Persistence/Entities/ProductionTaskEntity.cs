namespace Craftsman.Infra.Persistence.Entities;

public sealed class ProductionTaskEntity : IPersistenceEntity<Guid>
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid OrderItemId { get; set; }

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    public int ProductionDurationHours { get; set; } = 1;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset PlannedStartAt { get; set; }

    public DateTimeOffset PlannedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
