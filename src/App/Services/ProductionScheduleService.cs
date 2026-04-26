using Craftsman.App.Models;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Production.Repositories;
using Craftsman.Domain.Repositories;

namespace Craftsman.App.Services;

public sealed class ProductionScheduleService
{
    private readonly IProductionTaskRepository productionTaskRepository;
    private readonly IProductRepository productRepository;
    private readonly IUnitOfWork unitOfWork;

    public ProductionScheduleService(
        IProductionTaskRepository productionTaskRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        this.productionTaskRepository = productionTaskRepository;
        this.productRepository = productRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<ProductionTaskListItemViewModel>> ListAsync(
        ProductionTaskStatus? status = null,
        DateOnly? plannedDate = null,
        CancellationToken cancellationToken = default)
    {
        var tasks = await productionTaskRepository.ListAsync(status, plannedDate: null, cancellationToken);
        var products = await productRepository.ListAsync(cancellationToken);
        var durationsByProduct = products.ToDictionary(product => product.Id, product => product.ProductionDurationDays);

        var viewModels = tasks
            .Select(task => ToViewModel(task, durationsByProduct.GetValueOrDefault(task.ProductId, 1)))
            .ToList();

        if (plannedDate is not null)
        {
            viewModels = viewModels
                .Where(task =>
                {
                    var endDate = DateOnly.FromDateTime(task.PlannedAt.ToLocalTime().Date);
                    var startDate = endDate.AddDays(-(Math.Max(task.ProductionDurationDays, 1) - 1));

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

    public static ProductionTaskListItemViewModel ToViewModel(ProductionTask task, int productionDurationDays = 1)
    {
        return new ProductionTaskListItemViewModel(
            task.Id,
            task.OrderId,
            task.OrderItemId,
            task.ProductId,
            task.Quantity,
            productionDurationDays,
            task.Status.ToString(),
            task.PlannedAt,
            task.StartedAt,
            task.CompletedAt);
    }
}
