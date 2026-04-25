using Craftsman.App.Models;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Production.Repositories;
using Craftsman.Domain.Repositories;

namespace Craftsman.App.Services;

public sealed class ProductionScheduleService
{
    private readonly IProductionTaskRepository productionTaskRepository;
    private readonly IUnitOfWork unitOfWork;

    public ProductionScheduleService(IProductionTaskRepository productionTaskRepository, IUnitOfWork unitOfWork)
    {
        this.productionTaskRepository = productionTaskRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<ProductionTaskListItemViewModel>> ListAsync(
        ProductionTaskStatus? status = null,
        DateOnly? plannedDate = null,
        CancellationToken cancellationToken = default)
    {
        var tasks = await productionTaskRepository.ListAsync(status, plannedDate, cancellationToken);

        return tasks.Select(ToViewModel).ToList().AsReadOnly();
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

    public static ProductionTaskListItemViewModel ToViewModel(ProductionTask task)
    {
        return new ProductionTaskListItemViewModel(
            task.Id,
            task.OrderId,
            task.OrderItemId,
            task.ProductId,
            task.Quantity,
            task.Status.ToString(),
            task.PlannedAt,
            task.StartedAt,
            task.CompletedAt);
    }
}
