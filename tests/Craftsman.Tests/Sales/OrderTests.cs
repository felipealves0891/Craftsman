using Craftsman.Domain.Events;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Domain.Sales.Services;

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

    [Fact]
    public void Shipping_date_must_be_future_in_utc_when_informed()
    {
        var exception = Assert.Throws<ArgumentException>(() => new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "MAN-1"),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Caneca", 1, new Money(20, "BRL"))],
            shippingDate: DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.Contains("future", exception.Message);
    }

    [Fact]
    public void Customer_normalizer_removes_spaces_and_control_characters()
    {
        var normalized = CustomerNormalizer.NormalizeName("  Ana\tMaria\nSilva  ");

        Assert.Equal("ANAMARIASILVA", normalized);
    }

    [Fact]
    public void Order_accepts_optional_customer_and_discount()
    {
        var customer = new Customer(Guid.NewGuid(), "Ana Maria", "(11) 99999-0000", "ana@example.com");
        var discount = new OrderDiscount("Desconto comercial", new Money(5, "BRL"));

        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "MAN-1"),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Caneca", 1, new Money(20, "BRL"))],
            customer: customer,
            discount: discount);

        Assert.Equal(customer.Id, order.CustomerId);
        Assert.Equal("Ana Maria", order.Customer?.Name);
        Assert.Equal(5, order.Discount?.Amount.Amount);
    }

    [Fact]
    public void Discount_requires_description_when_amount_is_positive()
    {
        var exception = Assert.Throws<ArgumentException>(() => new OrderDiscount(string.Empty, new Money(1, "BRL")));

        Assert.Contains("description", exception.Message);
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
