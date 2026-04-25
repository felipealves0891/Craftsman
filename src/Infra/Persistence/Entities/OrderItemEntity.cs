namespace Craftsman.Infra.Persistence.Entities;

public sealed class OrderItemEntity : IPersistenceEntity<Guid>
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public OrderEntity? Order { get; set; }

    public string ExternalItemId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPriceAmount { get; set; }

    public string UnitPriceCurrency { get; set; } = string.Empty;

    public Guid? ProductId { get; set; }
}
