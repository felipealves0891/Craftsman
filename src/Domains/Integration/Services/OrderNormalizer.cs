using Craftsman.Domain.Integration.Models;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;

namespace Craftsman.Domain.Integration.Services;

public sealed class OrderNormalizer : IOrderNormalizer
{
    public Order Normalize(RawOrder rawOrder)
    {
        if (rawOrder is null)
        {
            throw new ArgumentNullException(nameof(rawOrder));
        }

        if (rawOrder.Items.Count == 0)
        {
            throw new InvalidOperationException("Raw order must contain at least one item.");
        }

        var items = rawOrder.Items.Select(item => new OrderItem(
            Guid.NewGuid(),
            item.ExternalItemId,
            item.Description,
            item.Quantity,
            new Money(item.UnitPrice, "BRL")));

        return new Order(
            Guid.NewGuid(),
            new OrderOrigin(rawOrder.Source, rawOrder.ExternalOrderId),
            new CustomerInfo(rawOrder.CustomerName, rawOrder.CustomerEmail),
            items);
    }
}
