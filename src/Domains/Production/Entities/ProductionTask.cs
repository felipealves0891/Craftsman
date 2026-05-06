using Craftsman.Domain.Events;

namespace Craftsman.Domain.Production.Entities;

public sealed class ProductionTask
{
    private readonly List<IDomainEvent> domainEvents = [];

    public Guid Id { get; }

    public Guid OrderId { get; }

    public Guid OrderItemId { get; }

    public Guid ProductId { get; }

    public int Quantity { get; }

    public int ProductionDurationHours { get; }

    public ProductionTaskStatus Status { get; private set; }

    public DateTimeOffset PlannedStartAt { get; }

    public DateTimeOffset PlannedAt { get; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => domainEvents.AsReadOnly();

    public ProductionTask(
        Guid id,
        Guid orderId,
        Guid orderItemId,
        Guid productId,
        int quantity,
        int productionDurationHours = 1,
        ProductionTaskStatus status = ProductionTaskStatus.Planned,
        DateTimeOffset? plannedStartAt = null,
        DateTimeOffset? plannedAt = null,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? completedAt = null,
        bool raisePlannedEvent = true)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Production task id is required.", nameof(id));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order id is required.", nameof(orderId));
        }

        if (orderItemId == Guid.Empty)
        {
            throw new ArgumentException("Order item id is required.", nameof(orderItemId));
        }

        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.", nameof(productId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (productionDurationHours <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productionDurationHours), "Production duration must be greater than zero.");
        }

        Id = id;
        OrderId = orderId;
        OrderItemId = orderItemId;
        ProductId = productId;
        Quantity = quantity;
        ProductionDurationHours = productionDurationHours;
        Status = status;
        PlannedStartAt = plannedStartAt ?? plannedAt ?? DateTimeOffset.UtcNow;
        PlannedAt = plannedAt ?? DateTimeOffset.UtcNow;
        StartedAt = startedAt;
        CompletedAt = completedAt;

        if (raisePlannedEvent)
        {
            domainEvents.Add(new ProductionPlannedEvent(Id, OrderId));
        }
    }

    public void Start()
    {
        EnsureStatus(ProductionTaskStatus.Planned);
        Status = ProductionTaskStatus.InProduction;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        EnsureStatus(ProductionTaskStatus.InProduction);
        Status = ProductionTaskStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status is ProductionTaskStatus.Completed or ProductionTaskStatus.Cancelled)
        {
            throw new InvalidOperationException($"Production task cannot be cancelled from status {Status}.");
        }

        Status = ProductionTaskStatus.Cancelled;
    }

    public void ClearDomainEvents()
    {
        domainEvents.Clear();
    }

    private void EnsureStatus(ProductionTaskStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"Production task cannot move from {Status}; expected {expected}.");
        }
    }
}
