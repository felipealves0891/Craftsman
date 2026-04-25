using Craftsman.Domain.Services;
using Craftsman.Domain.Shipping.Entities;
using Craftsman.Domain.Shipping.Repositories;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Infra.Repositories;

public sealed class ShipmentRepository : IShipmentRepository
{
    private readonly IApplicationCache? cache;
    private readonly AppDbContext dbContext;

    public ShipmentRepository(AppDbContext dbContext, IApplicationCache? cache = null)
    {
        this.dbContext = dbContext;
        this.cache = cache;
    }

    public async Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Shipments.FirstOrDefaultAsync(shipment => shipment.Id == id, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    public async Task<IReadOnlyCollection<Shipment>> ListAsync(ShipmentStatus? status = null, CancellationToken cancellationToken = default)
    {
        var key = status is null ? "shipping:list:all" : $"shipping:list:{status}";

        if (cache is not null)
        {
            return await cache.GetOrCreateAsync(key, token => ListCoreAsync(status, token), cancellationToken: cancellationToken);
        }

        return await ListCoreAsync(status, cancellationToken);
    }

    public async Task AddAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        await dbContext.Shipments.AddAsync(ToEntity(shipment), cancellationToken);
        cache?.RemoveByPrefix("shipping:");
    }

    public Task UpdateAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        dbContext.Shipments.Update(ToEntity(shipment));
        cache?.RemoveByPrefix("shipping:");
        return Task.CompletedTask;
    }

    private async Task<IReadOnlyCollection<Shipment>> ListCoreAsync(ShipmentStatus? status, CancellationToken cancellationToken)
    {
        var query = dbContext.Shipments.AsQueryable();

        if (status is not null)
        {
            query = query.Where(shipment => shipment.Status == status.ToString());
        }

        var entities = await query.OrderByDescending(shipment => shipment.CreatedAt).ToListAsync(cancellationToken);

        return entities.Select(ToModel).ToList().AsReadOnly();
    }

    private static Shipment ToModel(ShipmentEntity entity)
    {
        return new Shipment(
            entity.Id,
            entity.OrderId,
            entity.TrackingCode,
            Enum.Parse<ShipmentStatus>(entity.Status),
            entity.CreatedAt,
            entity.ShippedAt,
            entity.DeliveredAt,
            raiseCreatedEvent: false);
    }

    private static ShipmentEntity ToEntity(Shipment shipment)
    {
        return new ShipmentEntity
        {
            Id = shipment.Id,
            OrderId = shipment.OrderId,
            TrackingCode = shipment.TrackingCode,
            Status = shipment.Status.ToString(),
            CreatedAt = shipment.CreatedAt,
            ShippedAt = shipment.ShippedAt,
            DeliveredAt = shipment.DeliveredAt
        };
    }
}
