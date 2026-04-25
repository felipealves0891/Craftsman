using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Inventory.Repositories;
using Craftsman.Domain.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Infra.Repositories;

public sealed class StockMovementRepository : IStockMovementRepository
{
    private readonly IApplicationCache? cache;
    private readonly AppDbContext dbContext;

    public StockMovementRepository(AppDbContext dbContext, IApplicationCache? cache = null)
    {
        this.dbContext = dbContext;
        this.cache = cache;
    }

    public async Task AddAsync(StockMovement movement, CancellationToken cancellationToken = default)
    {
        await dbContext.StockMovements.AddAsync(ToEntity(movement), cancellationToken);
        cache?.Remove($"inventory:balance:{movement.RawMaterialId}");
        cache?.RemoveByPrefix("inventory:movements:");
    }

    public async Task<IReadOnlyCollection<StockMovement>> GetByRawMaterialAsync(Guid rawMaterialId, CancellationToken cancellationToken = default)
    {
        var movements = await dbContext.StockMovements
            .Where(movement => movement.RawMaterialId == rawMaterialId)
            .OrderBy(movement => movement.OccurredAt)
            .ToListAsync(cancellationToken);

        return movements.Select(ToModel).ToList().AsReadOnly();
    }

    public async Task<decimal> GetBalanceAsync(Guid rawMaterialId, CancellationToken cancellationToken = default)
    {
        var movements = await GetByRawMaterialAsync(rawMaterialId, cancellationToken);

        return movements.Sum(movement => movement.SignedQuantity);
    }

    public async Task<IReadOnlyCollection<StockMovement>> ListAsync(CancellationToken cancellationToken = default)
    {
        var movements = await dbContext.StockMovements
            .OrderBy(movement => movement.OccurredAt)
            .ToListAsync(cancellationToken);

        return movements.Select(ToModel).ToList().AsReadOnly();
    }

    public async Task<decimal> GetAverageUnitCostAsync(Guid rawMaterialId, CancellationToken cancellationToken = default)
    {
        var inboundMovements = await dbContext.StockMovements
            .Where(movement => movement.RawMaterialId == rawMaterialId && movement.Type == StockMovementType.Inbound.ToString())
            .ToListAsync(cancellationToken);

        var totalQuantity = inboundMovements.Sum(movement => movement.Quantity);

        if (totalQuantity == 0)
        {
            return 0;
        }

        return inboundMovements.Sum(movement => movement.Quantity * movement.UnitCostAmount) / totalQuantity;
    }

    private static StockMovement ToModel(StockMovementEntity entity)
    {
        return new StockMovement(
            entity.Id,
            entity.RawMaterialId,
            Enum.Parse<StockMovementType>(entity.Type),
            entity.Quantity,
            entity.Reason,
            entity.BusinessReference,
            entity.OccurredAt,
            entity.UnitCostAmount);
    }

    private static StockMovementEntity ToEntity(StockMovement movement)
    {
        return new StockMovementEntity
        {
            Id = movement.Id,
            RawMaterialId = movement.RawMaterialId,
            Type = movement.Type.ToString(),
            Quantity = movement.Quantity,
            UnitCostAmount = movement.UnitCostAmount,
            Reason = movement.Reason,
            BusinessReference = movement.BusinessReference,
            OccurredAt = movement.OccurredAt
        };
    }
}
