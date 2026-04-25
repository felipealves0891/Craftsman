using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Inventory.Repositories;

namespace Craftsman.Domain.Inventory.Services;

public sealed class InventoryService
{
    private readonly IRawMaterialRepository rawMaterialRepository;
    private readonly IStockMovementRepository stockMovementRepository;

    public InventoryService(IRawMaterialRepository rawMaterialRepository, IStockMovementRepository stockMovementRepository)
    {
        this.rawMaterialRepository = rawMaterialRepository;
        this.stockMovementRepository = stockMovementRepository;
    }

    public async Task<MaterialAvailabilityResult> CheckAvailabilityAsync(
        IEnumerable<MaterialRequirement> requirements,
        CancellationToken cancellationToken = default)
    {
        var missingMaterials = new List<MissingMaterial>();

        foreach (var requirement in requirements)
        {
            var rawMaterial = await rawMaterialRepository.GetByIdAsync(requirement.RawMaterialId, cancellationToken);
            var availableQuantity = rawMaterial?.CanBeConsumed == true
                ? await stockMovementRepository.GetBalanceAsync(requirement.RawMaterialId, cancellationToken)
                : 0;

            if (availableQuantity < requirement.RequiredQuantity)
            {
                missingMaterials.Add(new MissingMaterial(requirement.RawMaterialId, requirement.RequiredQuantity, availableQuantity));
            }
        }

        return missingMaterials.Count == 0
            ? MaterialAvailabilityResult.Available()
            : new MaterialAvailabilityResult(missingMaterials);
    }

    public async Task ConsumeForProductionAsync(
        IEnumerable<MaterialRequirement> requirements,
        Guid productionTaskId,
        CancellationToken cancellationToken = default)
    {
        var materialRequirements = requirements.ToList();
        var availability = await CheckAvailabilityAsync(materialRequirements, cancellationToken);

        if (!availability.IsAvailable)
        {
            throw new InvalidOperationException("Cannot consume materials when there is insufficient stock.");
        }

        foreach (var requirement in materialRequirements)
        {
            var averageUnitCost = await stockMovementRepository.GetAverageUnitCostAsync(requirement.RawMaterialId, cancellationToken);

            await stockMovementRepository.AddAsync(
                new StockMovement(
                    Guid.NewGuid(),
                    requirement.RawMaterialId,
                    StockMovementType.Outbound,
                    requirement.RequiredQuantity,
                    "Production consumption",
                    productionTaskId.ToString(),
                    unitCostAmount: averageUnitCost),
                cancellationToken);
        }
    }
}
