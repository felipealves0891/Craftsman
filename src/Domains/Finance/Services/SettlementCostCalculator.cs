using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Production.Entities;

namespace Craftsman.Domain.Finance.Services;

public static class SettlementCostCalculator
{
    public static decimal CalculateMaterialCost(
        IEnumerable<ProductionTask> productionTasks,
        IEnumerable<StockMovement> stockMovements)
    {
        return productionTasks.Sum(task =>
            stockMovements
                .Where(movement => movement.BusinessReference == task.Id.ToString())
                .Sum(movement => movement.TotalCostAmount));
    }

}
