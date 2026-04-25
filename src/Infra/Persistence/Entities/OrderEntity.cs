namespace Craftsman.Infra.Persistence.Entities;

public sealed class OrderEntity : IPersistenceEntity<Guid>
{
    public Guid Id { get; set; }

    public string Source { get; set; } = string.Empty;

    public string ExternalOrderId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string? CustomerEmail { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public List<OrderItemEntity> Items { get; set; } = [];
}
