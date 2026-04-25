using Craftsman.Domain.Production.Entities;

namespace Craftsman.Domain.Production.Repositories;

public interface IProductionTaskRepository
{
    Task<ProductionTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProductionTask>> ListAsync(ProductionTaskStatus? status = null, DateOnly? plannedDate = null, CancellationToken cancellationToken = default);

    Task AddAsync(ProductionTask productionTask, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProductionTask productionTask, CancellationToken cancellationToken = default);
}
