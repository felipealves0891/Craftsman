using Craftsman.Domain.Inventory.Entities;

namespace Craftsman.Domain.Inventory.Repositories;

public interface IRawMaterialRepository
{
    Task<RawMaterial?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(RawMaterial rawMaterial, CancellationToken cancellationToken = default);

    Task UpdateAsync(RawMaterial rawMaterial, CancellationToken cancellationToken = default);
}
