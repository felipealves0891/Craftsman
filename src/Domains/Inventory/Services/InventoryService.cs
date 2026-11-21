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
        var availability = await GetAvailabilityAsync(requirements, cancellationToken);
        var missingMaterials = availability
            .Where(material => !material.IsAvailable)
            .Select(material => new MissingMaterial(
                material.RawMaterialId,
                material.RequiredQuantity,
                material.AvailableQuantity))
            .ToList();

        return missingMaterials.Count == 0
            ? MaterialAvailabilityResult.Available()
            : new MaterialAvailabilityResult(missingMaterials);
    }

    public async Task<IReadOnlyCollection<MaterialAvailability>> GetAvailabilityAsync(
        IEnumerable<MaterialRequirement> requirements,
        CancellationToken cancellationToken = default)
    {
        var result = new List<MaterialAvailability>();
        foreach (var requirement in requirements)
        {
            var rawMaterial = await rawMaterialRepository.GetByIdAsync(requirement.RawMaterialId, cancellationToken);
            var isConfigured = rawMaterial?.CanBeConsumed == true;
            var availableQuantity = isConfigured
                ? await stockMovementRepository.GetBalanceAsync(requirement.RawMaterialId, cancellationToken)
                : 0;

            result.Add(new MaterialAvailability(
                requirement.RawMaterialId,
                rawMaterial?.Name ?? "Matéria-prima não cadastrada",
                rawMaterial?.UnitOfMeasure ?? "-",
                requirement.RequiredQuantity,
                availableQuantity,
                isConfigured));
        }

        return result.AsReadOnly();
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
