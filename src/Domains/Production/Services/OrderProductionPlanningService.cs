using Craftsman.Domain.Events;
using Craftsman.Domain.Production.Repositories;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.Repositories;

namespace Craftsman.Domain.Production.Services;

public sealed class OrderProductionPlanningService
{
    private readonly IDomainEventPublisher domainEventPublisher;
    private readonly IOrderRepository orderRepository;
    private readonly IProductionPlanner productionPlanner;
    private readonly IProductionTaskRepository productionTaskRepository;
    private readonly IUnitOfWork unitOfWork;

    public OrderProductionPlanningService(
        IOrderRepository orderRepository,
        IProductionPlanner productionPlanner,
        IProductionTaskRepository productionTaskRepository,
        IUnitOfWork unitOfWork,
        IDomainEventPublisher domainEventPublisher)
    {
        this.orderRepository = orderRepository;
        this.productionPlanner = productionPlanner;
        this.productionTaskRepository = productionTaskRepository;
        this.unitOfWork = unitOfWork;
        this.domainEventPublisher = domainEventPublisher;
    }

    public async Task<bool> TryPlanAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null || order.Items.Any(item => item.ProductId is null))
        {
            return false;
        }

        var existingTasks = await productionTaskRepository.ListAsync(cancellationToken: cancellationToken);
        if (existingTasks.Any(task => task.OrderId == orderId))
        {
            return false;
        }

        IReadOnlyCollection<Entities.ProductionTask> productionTasks;
        try
        {
            productionTasks = await productionPlanner.PlanAsync(order, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        foreach (var productionTask in productionTasks)
        {
            await productionTaskRepository.AddAsync(productionTask, cancellationToken);

            foreach (var domainEvent in productionTask.DomainEvents)
            {
                await domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
            }

            productionTask.ClearDomainEvents();
        }

        if (order.Status == OrderStatus.Normalized)
        {
            order.MarkReadyForProduction();
            await orderRepository.UpdateAsync(order, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return productionTasks.Count > 0;
    }
}
