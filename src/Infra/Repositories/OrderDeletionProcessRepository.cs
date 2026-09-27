using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.Repositories;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Infra.Repositories;

public sealed class OrderDeletionProcessRepository : IOrderDeletionProcessRepository
{
    private readonly AppDbContext dbContext;

    public OrderDeletionProcessRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<OrderDeletionProcess?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.OrderDeletionProcesses
            .AsNoTracking()
            .FirstOrDefaultAsync(process => process.Id == id, cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    public async Task AddAsync(OrderDeletionProcess process, CancellationToken cancellationToken = default)
    {
        await dbContext.OrderDeletionProcesses.AddAsync(ToEntity(process), cancellationToken);
    }

    public async Task UpdateAsync(OrderDeletionProcess process, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.OrderDeletionProcesses
            .FirstOrDefaultAsync(existing => existing.Id == process.Id, cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException($"Order deletion process '{process.Id}' was not found.");
        }

        MapToEntity(process, entity);
    }

    private static OrderDeletionProcess ToModel(OrderDeletionProcessEntity entity)
    {
        return new OrderDeletionProcess(
            entity.Id,
            entity.OrderId,
            entity.RequestedByUserId,
            entity.RequestedByUserName,
            entity.RequestedAt,
            entity.Source,
            entity.ExternalOrderId,
            entity.Status,
            entity.ShippingDate,
            entity.ItemCount,
            entity.ItemSummaryJson,
            entity.ProductionTaskCount,
            entity.ShipmentCount,
            entity.HadFinancialSettlement,
            entity.CompletedAt);
    }

    private static OrderDeletionProcessEntity ToEntity(OrderDeletionProcess process)
    {
        var entity = new OrderDeletionProcessEntity();
        MapToEntity(process, entity);
        return entity;
    }

    private static void MapToEntity(OrderDeletionProcess process, OrderDeletionProcessEntity entity)
    {
        entity.Id = process.Id;
        entity.OrderId = process.OrderId;
        entity.RequestedByUserId = process.RequestedByUserId;
        entity.RequestedByUserName = process.RequestedByUserName;
        entity.RequestedAt = process.RequestedAt;
        entity.Source = process.Source;
        entity.ExternalOrderId = process.ExternalOrderId;
        entity.Status = process.Status;
        entity.ShippingDate = process.ShippingDate;
        entity.ItemCount = process.ItemCount;
        entity.ItemSummaryJson = process.ItemSummaryJson;
        entity.ProductionTaskCount = process.ProductionTaskCount;
        entity.ShipmentCount = process.ShipmentCount;
        entity.HadFinancialSettlement = process.HadFinancialSettlement;
        entity.CompletedAt = process.CompletedAt;
    }
}
