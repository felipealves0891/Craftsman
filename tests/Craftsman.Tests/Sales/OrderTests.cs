using Craftsman.Domain.Events;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;

namespace Craftsman.Tests.Sales;

public sealed class OrderTests
{
    [Fact]
    public void New_order_records_origin_as_metadata_and_raises_normalized_event()
    {
        var order = CreateOrder();

        Assert.Equal("Shopee", order.Origin.Source);
        Assert.Equal("SO-1", order.Origin.ExternalOrderId);
        Assert.Equal(OrderStatus.Normalized, order.Status);
        Assert.Contains(order.DomainEvents, domainEvent => domainEvent is OrderNormalizedEvent);
    }

    [Fact]
    public void Invalid_status_transition_is_blocked()
    {
        var order = CreateOrder();

        var exception = Assert.Throws<InvalidOperationException>(order.MarkDelivered);

        Assert.Contains("expected Shipped", exception.Message);
    }

    private static Order CreateOrder()
    {
        return new Order(
            Guid.NewGuid(),
            new OrderOrigin("Shopee", "SO-1"),
            [
                new OrderItem(Guid.NewGuid(), "SKU-1", "Caneca", 2, new Money(35, "BRL"))
            ]);
    }
}
