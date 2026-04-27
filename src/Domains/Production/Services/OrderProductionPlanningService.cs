using Craftsman.Domain.Events;
using Craftsman.Domain.Production.Repositories;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.Repositories;
using Microsoft.Extensions.Logging;

namespace Craftsman.Domain.Production.Services;

public sealed class OrderProductionPlanningService
{
    private readonly IDomainEventPublisher domainEventPublisher;
    private readonly ILogger<OrderProductionPlanningService> logger;
    private readonly IOrderRepository orderRepository;
    private readonly IProductionPlanner productionPlanner;
    private readonly IProductionTaskRepository productionTaskRepository;
    private readonly IUnitOfWork unitOfWork;

    public OrderProductionPlanningService(
        IOrderRepository orderRepository,
        IProductionPlanner productionPlanner,
        IProductionTaskRepository productionTaskRepository,
        IUnitOfWork unitOfWork,
        IDomainEventPublisher domainEventPublisher,
        ILogger<OrderProductionPlanningService> logger)
    {
        this.orderRepository = orderRepository;
        this.productionPlanner = productionPlanner;
        this.productionTaskRepository = productionTaskRepository;
        this.unitOfWork = unitOfWork;
        this.domainEventPublisher = domainEventPublisher;
        this.logger = logger;

        this.logger = logger;
    }

    public async Task<bool> TryPlanAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null || order.Items.Any(item => item.ProductId is null))
        {
            logger.LogWarning("Order<{0}> not found or has items without product ID.", orderId);
            return false;
        }

        var existingTasks = await productionTaskRepository.ListAsync(cancellationToken: cancellationToken);
        if (existingTasks.Any(task => task.OrderId == orderId))
        {
            logger.LogWarning("Order<{0}> already has production tasks.", orderId);
            return false;
        }

        IReadOnlyCollection<Entities.ProductionTask> productionTasks;
        try
        {
            productionTasks = await productionPlanner.PlanAsync(order, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Failed to plan production for order<{0}> by reason: {1}", orderId, ex.Message);
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
