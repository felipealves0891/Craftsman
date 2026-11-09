using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.ProductCatalog.Entities;
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

    public static decimal CalculateLaborCost(
        IEnumerable<ProductionTask> productionTasks,
        IReadOnlyDictionary<Guid, Product> products)
    {
        return productionTasks.Sum(task =>
        {
            if (!products.TryGetValue(task.ProductId, out var product))
            {
                throw new InvalidOperationException($"Product '{task.ProductId}' for production task '{task.Id}' was not found.");
            }

            return task.ProductionDurationHours * product.HourlyRate;
        });
    }
}
