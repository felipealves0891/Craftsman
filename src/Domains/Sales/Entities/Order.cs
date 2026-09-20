using Craftsman.Domain.Events;
using Craftsman.Domain.Sales.ObjectValues;

namespace Craftsman.Domain.Sales.Entities;

public sealed class Order
{
    private readonly List<OrderItem> items;
    private readonly List<IDomainEvent> domainEvents = [];

    public Guid Id { get; }

    public OrderOrigin Origin { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateOnly? ShippingDate { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => items.AsReadOnly();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => domainEvents.AsReadOnly();

    public Order(
        Guid id,
        OrderOrigin origin,
        IEnumerable<OrderItem> items,
        OrderStatus status = OrderStatus.Normalized,
        DateTimeOffset? createdAt = null,
        DateOnly? shippingDate = null,
        bool raiseNormalizedEvent = true)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Order id is required.", nameof(id));
        }

        this.items = items?.ToList() ?? throw new ArgumentNullException(nameof(items));

        if (this.items.Count == 0)
        {
            throw new ArgumentException("Order must contain at least one item.", nameof(items));
        }

        Id = id;
        Origin = origin;
        Status = status;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        if (createdAt is null && shippingDate is not null && shippingDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ArgumentException("Shipping date must be in the future.", nameof(shippingDate));
        }

        ShippingDate = shippingDate;

        if (raiseNormalizedEvent)
        {
            domainEvents.Add(new OrderNormalizedEvent(Id, Origin.Source, Origin.ExternalOrderId));
        }
    }

    public void MarkReadyForProduction()
    {
        EnsureStatus(OrderStatus.Normalized);
        Status = OrderStatus.ReadyForProduction;
    }

    public void StartProduction()
    {
        EnsureStatus(OrderStatus.ReadyForProduction);
        Status = OrderStatus.InProduction;
    }

    public void MarkShipped()
    {
        EnsureStatus(OrderStatus.InProduction);
        Status = OrderStatus.Shipped;
    }

    public void MarkDelivered()
    {
        EnsureStatus(OrderStatus.Shipped);
        Status = OrderStatus.Delivered;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Delivered or OrderStatus.Cancelled)
        {
            throw new InvalidOperationException($"Order cannot be cancelled from status {Status}.");
        }

        Status = OrderStatus.Cancelled;
    }

    public void Replace(OrderOrigin origin, IEnumerable<OrderItem> replacementItems, DateOnly? shippingDate)
    {
        var newItems = replacementItems?.ToList() ?? throw new ArgumentNullException(nameof(replacementItems));

        if (newItems.Count == 0)
        {
            throw new ArgumentException("Order must contain at least one item.", nameof(replacementItems));
        }

        if (shippingDate is not null && shippingDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ArgumentException("Shipping date must be in the future.", nameof(shippingDate));
        }

        Origin = origin;
        ShippingDate = shippingDate;
        items.Clear();
        items.AddRange(newItems);
    }

    public void MarkNormalized()
    {
        if (Status is OrderStatus.InProduction or OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled)
        {
            throw new InvalidOperationException($"Order cannot be normalized from status {Status}.");
        }

        Status = OrderStatus.Normalized;
    }

    public void ClearDomainEvents()
    {
        domainEvents.Clear();
    }

    private void EnsureStatus(OrderStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"Order cannot move from {Status}; expected {expected}.");
        }
    }
}
