using Craftsman.App.Models;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Production.Repositories;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Sales.Repositories;

namespace Craftsman.App.Services;

public sealed class ProductionScheduleService
{
    private readonly IProductionTaskRepository productionTaskRepository;
    private readonly IProductRepository productRepository;
    private readonly IOrderRepository orderRepository;
    private readonly IUnitOfWork unitOfWork;

    public ProductionScheduleService(
        IProductionTaskRepository productionTaskRepository,
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork)
    {
        this.productionTaskRepository = productionTaskRepository;
        this.productRepository = productRepository;
        this.orderRepository = orderRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<ProductionTaskListItemViewModel>> ListAsync(
        ProductionTaskStatus? status = null,
        DateOnly? plannedDate = null,
        CancellationToken cancellationToken = default)
    {
        var tasks = await productionTaskRepository.ListAsync(status, plannedDate: null, cancellationToken);
        var orders = await orderRepository.ListAsync(cancellationToken);
        var ordersById = orders.ToDictionary(order => order.Id);

        var viewModels = tasks
            .Select(task =>
            {
                ordersById.TryGetValue(task.OrderId, out var order);
                var orderItem = order?.Items.FirstOrDefault(item => item.Id == task.OrderItemId);

                return ToViewModel(
                    task,
                    order is null ? task.OrderId.ToString()[..8] : $"{order.Origin.Source} {order.Origin.ExternalOrderId}",
                    orderItem?.Description ?? "Item sem descricao",
                    order?.Origin.ExternalOrderId ?? task.OrderId.ToString()[..8]);
            })
            .ToList();

        if (plannedDate is not null)
        {
            viewModels = viewModels
                .Where(task =>
                {
                    var startDate = DateOnly.FromDateTime(task.PlannedStartAt.ToLocalTime().Date);
                    var endDate = DateOnly.FromDateTime(task.PlannedAt.ToLocalTime().Date);

                    return plannedDate.Value >= startDate && plannedDate.Value <= endDate;
                })
                .ToList();
        }

        return viewModels.AsReadOnly();
    }

    public async Task AdvanceAsync(Guid id, string action, CancellationToken cancellationToken = default)
    {
        var task = await productionTaskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Production task was not found.");

        switch (action)
        {
            case "start":
                task.Start();
                break;
            case "complete":
                task.Complete();
                break;
            case "cancel":
                task.Cancel();
                break;
            default:
                throw new InvalidOperationException($"Unsupported production action '{action}'.");
        }

        await productionTaskRepository.UpdateAsync(task, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public static ProductionTaskListItemViewModel ToViewModel(
        ProductionTask task,
        string orderReference = "",
        string itemDescription = "",
        string externalOrderId = "")
    {
        return new ProductionTaskListItemViewModel(
            task.Id,
            task.OrderId,
            task.OrderItemId,
            task.ProductId,
            task.Quantity,
            task.ProductionDurationHours,
            task.Status.ToString(),
            task.PlannedStartAt,
            task.PlannedAt,
            task.StartedAt,
            task.CompletedAt,
            orderReference,
            itemDescription,
            externalOrderId);
    }
}
