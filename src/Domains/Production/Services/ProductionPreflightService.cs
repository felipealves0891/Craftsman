using Craftsman.Domain.Inventory.Services;
using Craftsman.Domain.Production.Repositories;
using Craftsman.Domain.Sales.Repositories;

namespace Craftsman.Domain.Production.Services;

public sealed class ProductionPreflightService
{
    private readonly IOrderRepository orderRepository;
    private readonly IProductionTaskRepository productionTaskRepository;
    private readonly ProductionPlanPreparationService preparationService;
    private readonly InventoryService inventoryService;

    public ProductionPreflightService(
        IOrderRepository orderRepository,
        IProductionTaskRepository productionTaskRepository,
        ProductionPlanPreparationService preparationService,
        InventoryService inventoryService)
    {
        this.orderRepository = orderRepository;
        this.productionTaskRepository = productionTaskRepository;
        this.preparationService = preparationService;
        this.inventoryService = inventoryService;
    }

    public async Task<ProductionPreflightResult> GetAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            return ProductionPreflightResult.Failed(ProductionPlanningFailureReason.OrderNotFound);
        }

        var existingTasks = await productionTaskRepository.ListAsync(cancellationToken: cancellationToken);
        if (existingTasks.Any(task => task.OrderId == orderId))
        {
            return ProductionPreflightResult.Failed(ProductionPlanningFailureReason.AlreadyPlanned);
        }

        var preparation = await preparationService.PrepareAsync(order, cancellationToken);
        if (!preparation.IsValid)
        {
            return ProductionPreflightResult.Failed(preparation.FailureReason);
        }

        var materials = await inventoryService.GetAvailabilityAsync(preparation.Requirements, cancellationToken);
        var result = materials
            .Select(material => new ProductionPreflightMaterial(
                material.RawMaterialId,
                material.Name,
                material.UnitOfMeasure,
                material.RequiredQuantity,
                material.AvailableQuantity,
                material.IsConfigured,
                material.IsAvailable,
                material.MissingQuantity))
            .ToList()
            .AsReadOnly();

        return ProductionPreflightResult.Success(result);
    }
}

public sealed record ProductionPreflightMaterial(
    Guid RawMaterialId,
    string Name,
    string UnitOfMeasure,
    decimal RequiredQuantity,
    decimal AvailableQuantity,
    bool IsConfigured,
    bool IsAvailable,
    decimal MissingQuantity);

public sealed class ProductionPreflightResult
{
    private ProductionPreflightResult(
        IReadOnlyCollection<ProductionPreflightMaterial> materials,
        ProductionPlanningFailureReason failureReason)
    {
        Materials = materials;
        FailureReason = failureReason;
    }

    public IReadOnlyCollection<ProductionPreflightMaterial> Materials { get; }

    public ProductionPlanningFailureReason FailureReason { get; }

    public bool CanConfirm => FailureReason == ProductionPlanningFailureReason.None
        && Materials.All(material => material.IsAvailable);

    public static ProductionPreflightResult Success(IReadOnlyCollection<ProductionPreflightMaterial> materials) =>
        new(materials, ProductionPlanningFailureReason.None);

    public static ProductionPreflightResult Failed(ProductionPlanningFailureReason failureReason) =>
        new([], failureReason);
}
