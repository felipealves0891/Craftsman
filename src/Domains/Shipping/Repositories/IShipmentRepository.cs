using Craftsman.Domain.Shipping.Entities;

namespace Craftsman.Domain.Shipping.Repositories;

public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Shipment>> ListAsync(ShipmentStatus? status = null, CancellationToken cancellationToken = default);

    Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default);

    Task UpdateAsync(Shipment shipment, CancellationToken cancellationToken = default);
}
