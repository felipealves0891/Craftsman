using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Sales.Entities;

namespace Craftsman.Domain.Production.Services;

public interface IProductionPlanner
{
    Task<IReadOnlyCollection<ProductionTask>> PlanAsync(Order order, CancellationToken cancellationToken = default);
}
