using Craftsman.Domain.Inventory.Entities;

namespace Craftsman.Domain.Inventory.Repositories;

public interface IStockMovementRepository
{
    Task AddAsync(StockMovement movement, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StockMovement>> GetByRawMaterialAsync(Guid rawMaterialId, CancellationToken cancellationToken = default);

    Task<decimal> GetBalanceAsync(Guid rawMaterialId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StockMovement>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<StockMovement>> ListByBusinessReferenceAsync(string businessReference, CancellationToken cancellationToken = default);

    Task<decimal> GetAverageUnitCostAsync(Guid rawMaterialId, CancellationToken cancellationToken = default);
}
