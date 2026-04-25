using Craftsman.Domain.Events;
using Craftsman.Domain.Sales.ObjectValues;

namespace Craftsman.Domain.Sales.Entities;

public sealed class Order
{
    private readonly List<OrderItem> items;
    private readonly List<IDomainEvent> domainEvents = [];

    public Guid Id { get; }

    public OrderOrigin Origin { get; }

    public CustomerInfo Customer { get; }

    public OrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public IReadOnlyCollection<OrderItem> Items => items.AsReadOnly();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => domainEvents.AsReadOnly();

    public Order(
        Guid id,
        OrderOrigin origin,
        CustomerInfo customer,
        IEnumerable<OrderItem> items,
        OrderStatus status = OrderStatus.Normalized,
        DateTimeOffset? createdAt = null,
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
        Customer = customer;
        Status = status;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;

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
