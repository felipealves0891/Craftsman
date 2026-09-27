using Craftsman.Domain.Sales.Entities;

namespace Craftsman.Domain.Sales.Repositories;

public interface IOrderDeletionProcessRepository
{
    Task<OrderDeletionProcess?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(OrderDeletionProcess process, CancellationToken cancellationToken = default);

    Task UpdateAsync(OrderDeletionProcess process, CancellationToken cancellationToken = default);
}
