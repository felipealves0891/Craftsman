using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;

namespace Craftsman.Domain.Sales.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Order?> GetByOriginAsync(OrderOrigin origin, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Order>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
