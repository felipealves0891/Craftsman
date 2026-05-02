using Craftsman.Domain.Sales.Entities;

namespace Craftsman.Domain.Sales.Repositories;

public interface IOrderSourceCatalogRepository
{
    Task<OrderSource?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<OrderSource>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(OrderSource source, CancellationToken cancellationToken = default);
}
