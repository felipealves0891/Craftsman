namespace Craftsman.Infra.Persistence.Entities;

public sealed class OrderEntity : IPersistenceEntity<Guid>
{
    public Guid Id { get; set; }

    public string Source { get; set; } = string.Empty;

    public string ExternalOrderId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateOnly? ShippingDate { get; set; }

    public Guid? CustomerId { get; set; }

    public CustomerEntity? Customer { get; set; }

    public string? DiscountDescription { get; set; }

    public decimal DiscountAmount { get; set; }

    public List<OrderItemEntity> Items { get; set; } = [];
}
