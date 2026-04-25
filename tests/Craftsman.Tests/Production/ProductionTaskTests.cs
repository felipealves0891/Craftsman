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

        var task = new ProductionTask(Guid.NewGuid(), orderId, orderItemId, productId, 2);

        Assert.Equal(orderId, task.OrderId);
        Assert.Equal(orderItemId, task.OrderItemId);
        Assert.Equal(productId, task.ProductId);
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
