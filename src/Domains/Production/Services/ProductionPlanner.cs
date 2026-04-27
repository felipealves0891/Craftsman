using Craftsman.Domain.Inventory.Services;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Sales.Entities;

namespace Craftsman.Domain.Production.Services;

public sealed class ProductionPlanner : IProductionPlanner
{
    private readonly InventoryService inventoryService;
    private readonly IProductRepository productRepository;

    public ProductionPlanner(IProductRepository productRepository, InventoryService inventoryService)
    {
        this.productRepository = productRepository;
        this.inventoryService = inventoryService;
    }

    public async Task<IReadOnlyCollection<ProductionTask>> PlanAsync(Order order, CancellationToken cancellationToken = default)
    {
        var productionTasks = new List<ProductionTask>();

        foreach (var item in order.Items)
        {
            if (item.ProductId is null)
            {
                throw new InvalidOperationException("Production cannot be planned without an internal product mapping.");
            }

            var product = await productRepository.GetByIdAsync(item.ProductId.Value, cancellationToken);

            if (product is null || !product.CanBePlannedForProduction)
            {
                throw new InvalidOperationException("Production cannot be planned without an active product and valid bill of materials.");
            }

            var requirements = BuildRequirements(product, item.Quantity);
            var availability = await inventoryService.CheckAvailabilityAsync(requirements, cancellationToken);

            if (!availability.IsAvailable)
            {
                throw new InvalidOperationException($"Production cannot be planned with insufficient material: {string.Join(", ", availability.MissingMaterials)}.");
            }

            var productionTask = new ProductionTask(Guid.NewGuid(), order.Id, item.Id, product.Id, item.Quantity);
            await inventoryService.ConsumeForProductionAsync(requirements, productionTask.Id, cancellationToken);
            productionTasks.Add(productionTask);
        }

        return productionTasks.AsReadOnly();
    }

    private static IReadOnlyCollection<MaterialRequirement> BuildRequirements(Product product, int quantity)
    {
        return product.BillOfMaterials
            .Select(item => new MaterialRequirement(item.RawMaterialId, item.QuantityPerUnit * quantity))
            .ToList()
            .AsReadOnly();
    }
}
