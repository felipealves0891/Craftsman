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
        var consolidatedMaterials = materials
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

        var availabilityByMaterialId = materials.ToDictionary(material => material.RawMaterialId);
        var products = preparation.PlannedItems
            .Select(item => new ProductionPreflightProduct(
                item.Product.Id,
                item.Product.Name,
                item.OrderItem.Quantity,
                item.Requirements
                    .Select(requirement =>
                    {
                        var availability = availabilityByMaterialId[requirement.RawMaterialId];
                        return new ProductionPreflightProductMaterial(
                            availability.RawMaterialId,
                            availability.Name,
                            availability.UnitOfMeasure,
                            requirement.RequiredQuantity,
                            availability.AvailableQuantity,
                            availability.MissingQuantity,
                            availability.IsConfigured,
                            availability.IsAvailable);
                    })
                    .ToList()
                    .AsReadOnly()))
            .ToList()
            .AsReadOnly();

        return ProductionPreflightResult.Success(consolidatedMaterials, products);
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

public sealed record ProductionPreflightProduct(
    Guid ProductId,
    string Name,
    int Quantity,
    IReadOnlyCollection<ProductionPreflightProductMaterial> Materials);

public sealed record ProductionPreflightProductMaterial(
    Guid RawMaterialId,
    string Name,
    string UnitOfMeasure,
    decimal RequiredQuantity,
    decimal AvailableQuantity,
    decimal MissingQuantity,
    bool IsConfigured,
    bool IsAvailable);

public sealed class ProductionPreflightResult
{
    private ProductionPreflightResult(
        IReadOnlyCollection<ProductionPreflightMaterial> materials,
        IReadOnlyCollection<ProductionPreflightProduct> products,
        ProductionPlanningFailureReason failureReason)
    {
        Materials = materials;
        Products = products;
        FailureReason = failureReason;
    }

    public IReadOnlyCollection<ProductionPreflightMaterial> Materials { get; }

    public IReadOnlyCollection<ProductionPreflightProduct> Products { get; }

    public ProductionPlanningFailureReason FailureReason { get; }

    public bool CanConfirm => FailureReason == ProductionPlanningFailureReason.None
        && Materials.All(material => material.IsAvailable);

    public static ProductionPreflightResult Success(
        IReadOnlyCollection<ProductionPreflightMaterial> materials,
        IReadOnlyCollection<ProductionPreflightProduct> products) =>
        new(materials, products, ProductionPlanningFailureReason.None);

    public static ProductionPreflightResult Failed(ProductionPlanningFailureReason failureReason) =>
        new([], [], failureReason);
}
