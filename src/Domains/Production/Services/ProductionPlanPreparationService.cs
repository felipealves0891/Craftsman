using Craftsman.Domain.Inventory.Services;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.Sales.Entities;

namespace Craftsman.Domain.Production.Services;

public sealed class ProductionPlanPreparationService
{
    private readonly IProductRepository productRepository;

    public ProductionPlanPreparationService(IProductRepository productRepository)
    {
        this.productRepository = productRepository;
    }

    public async Task<ProductionPlanPreparationResult> PrepareAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        var plannedItems = new List<PreparedProductionItem>();
        var requirements = new List<MaterialRequirement>();

        foreach (var item in order.Items)
        {
            if (item.ProductId is null)
            {
                return ProductionPlanPreparationResult.Failed(ProductionPlanningFailureReason.MissingProductMapping);
            }

            var product = await productRepository.GetByIdAsync(item.ProductId.Value, cancellationToken);
            if (product is null || !product.CanBePlannedForProduction)
            {
                return ProductionPlanPreparationResult.Failed(ProductionPlanningFailureReason.InvalidProductConfiguration);
            }

            var itemRequirements = product.BillOfMaterials
                .Select(billOfMaterialsItem => new MaterialRequirement(
                    billOfMaterialsItem.RawMaterialId,
                    billOfMaterialsItem.QuantityPerUnit * item.Quantity))
                .ToList()
                .AsReadOnly();

            plannedItems.Add(new PreparedProductionItem(item, product, itemRequirements));
            requirements.AddRange(itemRequirements);
        }

        var aggregatedRequirements = requirements
            .GroupBy(requirement => requirement.RawMaterialId)
            .Select(group => new MaterialRequirement(
                group.Key,
                group.Sum(requirement => requirement.RequiredQuantity)))
            .ToList()
            .AsReadOnly();

        return ProductionPlanPreparationResult.Success(plannedItems.AsReadOnly(), aggregatedRequirements);
    }

    public sealed record PreparedProductionItem(
        OrderItem OrderItem,
        Product Product,
        IReadOnlyCollection<MaterialRequirement> Requirements);
}

public sealed class ProductionPlanPreparationResult
{
    private ProductionPlanPreparationResult(
        IReadOnlyCollection<ProductionPlanPreparationService.PreparedProductionItem> plannedItems,
        IReadOnlyCollection<MaterialRequirement> requirements,
        ProductionPlanningFailureReason failureReason)
    {
        PlannedItems = plannedItems;
        Requirements = requirements;
        FailureReason = failureReason;
    }

    public bool IsValid => FailureReason == ProductionPlanningFailureReason.None;

    public IReadOnlyCollection<ProductionPlanPreparationService.PreparedProductionItem> PlannedItems { get; }

    public IReadOnlyCollection<MaterialRequirement> Requirements { get; }

    public ProductionPlanningFailureReason FailureReason { get; }

    public static ProductionPlanPreparationResult Success(
        IReadOnlyCollection<ProductionPlanPreparationService.PreparedProductionItem> plannedItems,
        IReadOnlyCollection<MaterialRequirement> requirements) =>
        new(plannedItems, requirements, ProductionPlanningFailureReason.None);

    public static ProductionPlanPreparationResult Failed(ProductionPlanningFailureReason failureReason) =>
        new([], [], failureReason);
}
