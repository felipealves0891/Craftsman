using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Production.Entities;

namespace Craftsman.Domain.Finance.Services;

public static class SettlementCostCalculator
{
    public static decimal CalculateMaterialCost(
        Guid orderId,
        IEnumerable<ProductionTask> productionTasks,
        IEnumerable<StockMovement> stockMovements)
    {
        var taskReferences = productionTasks
            .Select(task => task.Id.ToString())
            .ToHashSet(StringComparer.Ordinal);

        var productionMaterialCost = stockMovements
            .Where(movement => movement.BusinessReference is not null && taskReferences.Contains(movement.BusinessReference))
            .Sum(movement => movement.TotalCostAmount);

        var manualOutboundMaterialCost = stockMovements
            .Where(movement =>
                movement.OrderId == orderId &&
                movement.Type == StockMovementType.Outbound &&
                (movement.BusinessReference is null || !taskReferences.Contains(movement.BusinessReference)))
            .Sum(movement => movement.TotalCostAmount);

        return productionMaterialCost + manualOutboundMaterialCost;
    }

}
