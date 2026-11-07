using Craftsman.Domain.Events;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Inventory.Repositories;
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
    private readonly IStockMovementRepository stockMovementRepository;
    private readonly IUnitOfWork unitOfWork;

    public OrderProductionPlanningService(
        IOrderRepository orderRepository,
        IProductionPlanner productionPlanner,
        IProductionTaskRepository productionTaskRepository,
        IUnitOfWork unitOfWork,
        IDomainEventPublisher domainEventPublisher,
        ILogger<OrderProductionPlanningService> logger,
        IStockMovementRepository stockMovementRepository = null!)
    {
        this.orderRepository = orderRepository;
        this.productionPlanner = productionPlanner;
        this.productionTaskRepository = productionTaskRepository;
        this.stockMovementRepository = stockMovementRepository;
        this.unitOfWork = unitOfWork;
        this.domainEventPublisher = domainEventPublisher;
        this.logger = logger;

        this.logger = logger;
    }

    public async Task<bool> TryPlanAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Start planning production for order<{0}>.", orderId);
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

        logger.LogInformation("Found {0} production tasks for order<{1}>.", productionTasks.Count, orderId);
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
            logger.LogInformation("Marking order<{0}> as ready for production.", orderId);
            order.MarkReadyForProduction();
            await orderRepository.UpdateAsync(order, cancellationToken);
            logger.LogInformation("Order<{0}> is normalized, marking as ready for production.", orderId);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return productionTasks.Count > 0;
    }

    public async Task<bool> RevertAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new InvalidOperationException("Order was not found.");
        var productionTasks = await productionTaskRepository.ListByOrderAsync(orderId, cancellationToken);

        if (productionTasks.Any(task => task.Status == Entities.ProductionTaskStatus.Completed))
        {
            throw new InvalidOperationException("Order production planning cannot be reverted after a task is completed.");
        }

        if (productionTasks.Count == 0)
        {
            if (order.Status == OrderStatus.Normalized)
            {
                return false;
            }

            throw new InvalidOperationException($"Order production planning cannot be reverted from status {order.Status}.");
        }

        foreach (var productionTask in productionTasks)
        {
            var movements = await stockMovementRepository.ListByBusinessReferenceAsync(
                productionTask.Id.ToString(),
                cancellationToken);

            foreach (var movement in movements.Where(movement => movement.Type == StockMovementType.Outbound))
            {
                var compensationReference = $"production-reversal:{movement.Id}";
                if (await stockMovementRepository.ExistsByBusinessReferenceAsync(compensationReference, cancellationToken))
                {
                    continue;
                }

                await stockMovementRepository.AddAsync(
                    new StockMovement(
                        Guid.NewGuid(),
                        movement.RawMaterialId,
                        StockMovementType.Inbound,
                        movement.Quantity,
                        "Production planning reversal",
                        compensationReference,
                        unitCostAmount: movement.UnitCostAmount),
                    cancellationToken);
            }

            await productionTaskRepository.DeleteAsync(productionTask.Id, cancellationToken);
        }

        order.RevertProductionPlanning();
        await orderRepository.UpdateAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
