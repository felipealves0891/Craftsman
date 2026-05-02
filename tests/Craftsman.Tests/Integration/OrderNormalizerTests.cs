using Craftsman.Domain.Integration.Models;
using Craftsman.Domain.Integration.Services;
using Craftsman.Domain.Sales.Entities;

namespace Craftsman.Tests.Integration;

public sealed class OrderNormalizerTests
{
    [Fact]
    public void Normalizer_converts_raw_order_to_internal_order()
    {
        var normalizer = new OrderNormalizer();
        var rawOrder = new RawOrder(
            "Elo7",
            "E-100",
            [new RawOrderItem("EXT-1", "Produto externo", 3, 12.5m)]);

        var order = normalizer.Normalize(rawOrder);

        Assert.Equal(OrderStatus.Normalized, order.Status);
        Assert.Equal("Elo7", order.Origin.Source);
        Assert.Equal("E-100", order.Origin.ExternalOrderId);
        Assert.Single(order.Items);
        Assert.Equal("Produto externo", order.Items.Single().Description);
    }

    [Fact]
    public void Normalizer_rejects_raw_order_without_items()
    {
        var normalizer = new OrderNormalizer();
        var rawOrder = new RawOrder("Elo7", "E-101", []);

        var exception = Assert.Throws<InvalidOperationException>(() => normalizer.Normalize(rawOrder));

        Assert.Contains("at least one item", exception.Message);
    }
}
