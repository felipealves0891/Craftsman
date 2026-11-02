using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;

namespace Craftsman.Tests.Sales;

public sealed class OrderItemAndSourceTests
{
    [Fact]
    public void Order_item_trims_text_and_allows_product_assignment()
    {
        var productId = Guid.NewGuid();
        var item = new OrderItem(Guid.NewGuid(), " SKU-1 ", " Caneca ", 2, new Money(15, "BRL"));

        Assert.Equal("SKU-1", item.ExternalItemId);
        Assert.Equal("Caneca", item.Description);
        Assert.Equal(2, item.Quantity);
        Assert.Null(item.ProductId);

        item.AssignProduct(productId);

        Assert.Equal(productId, item.ProductId);
        Assert.Throws<ArgumentException>(() => item.AssignProduct(Guid.Empty));
    }

    [Fact]
    public void Order_item_rejects_invalid_identifiers_text_and_quantity()
    {
        var price = new Money(1, "BRL");

        Assert.Throws<ArgumentException>(() => new OrderItem(Guid.Empty, "SKU", "Produto", 1, price));
        Assert.Throws<ArgumentException>(() => new OrderItem(Guid.NewGuid(), " ", "Produto", 1, price));
        Assert.Throws<ArgumentException>(() => new OrderItem(Guid.NewGuid(), "SKU", " ", 1, price));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OrderItem(Guid.NewGuid(), "SKU", "Produto", 0, price));
    }

    [Fact]
    public void Order_source_trims_name_and_uses_given_timestamp()
    {
        var createdAt = DateTimeOffset.Parse("2026-10-15T12:00:00+00:00");
        var source = new OrderSource(Guid.NewGuid(), "  Shopee  ", createdAt);

        Assert.Equal("Shopee", source.Name);
        Assert.Equal(createdAt, source.CreatedAt);
        Assert.Throws<ArgumentException>(() => new OrderSource(Guid.Empty, "Shopee"));
        Assert.Throws<ArgumentException>(() => new OrderSource(Guid.NewGuid(), " "));
    }
}
