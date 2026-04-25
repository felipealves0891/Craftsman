using Craftsman.Domain.Inventory.Entities;

namespace Craftsman.Domain.Inventory.Repositories;

public interface IStockMovementRepository
{
    Task AddAsync(StockMovement movement, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StockMovement>> GetByRawMaterialAsync(Guid rawMaterialId, CancellationToken cancellationToken = default);

    Task<decimal> GetBalanceAsync(Guid rawMaterialId, CancellationToken cancellationToken = default);
}
