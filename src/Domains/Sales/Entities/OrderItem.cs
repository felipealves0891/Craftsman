using Craftsman.Domain.Sales.ObjectValues;

namespace Craftsman.Domain.Sales.Entities;

public sealed class OrderItem
{
    public Guid Id { get; }

    public string ExternalItemId { get; }

    public string Description { get; }

    public int Quantity { get; }

    public Money UnitPrice { get; }

    public Guid? ProductId { get; private set; }

    public OrderItem(Guid id, string externalItemId, string description, int quantity, Money unitPrice, Guid? productId = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Order item id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(externalItemId))
        {
            throw new ArgumentException("External item id is required.", nameof(externalItemId));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Item description is required.", nameof(description));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        Id = id;
        ExternalItemId = externalItemId.Trim();
        Description = description.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
        ProductId = productId;
    }

    public void AssignProduct(Guid productId)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.", nameof(productId));
        }

        ProductId = productId;
    }
}
