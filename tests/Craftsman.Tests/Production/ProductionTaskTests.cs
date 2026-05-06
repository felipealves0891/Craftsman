using Craftsman.Domain.Events;
using Craftsman.Domain.Production.Entities;

namespace Craftsman.Tests.Production;

public sealed class ProductionTaskTests
{
    [Fact]
    public void Production_task_records_traceability_and_planned_event()
    {
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var plannedStartAt = new DateTimeOffset(2026, 5, 10, 8, 0, 0, TimeSpan.Zero);
        var plannedAt = new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero);
        var task = new ProductionTask(
            Guid.NewGuid(),
            orderId,
            orderItemId,
            productId,
            2,
            4,
            plannedStartAt: plannedStartAt,
            plannedAt: plannedAt);

        Assert.Equal(orderId, task.OrderId);
        Assert.Equal(orderItemId, task.OrderItemId);
        Assert.Equal(productId, task.ProductId);
        Assert.Equal(4, task.ProductionDurationHours);
        Assert.Equal(plannedStartAt, task.PlannedStartAt);
        Assert.Equal(plannedAt, task.PlannedAt);
        Assert.Equal(ProductionTaskStatus.Planned, task.Status);
        Assert.Contains(task.DomainEvents, domainEvent => domainEvent is ProductionPlannedEvent);
    }

    [Fact]
    public void Invalid_status_transition_is_blocked()
    {
        var task = new ProductionTask(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);

        var exception = Assert.Throws<InvalidOperationException>(task.Complete);

        Assert.Contains("expected InProduction", exception.Message);
    }
}
