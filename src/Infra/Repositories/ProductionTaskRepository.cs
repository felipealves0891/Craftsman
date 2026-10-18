using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Production.Repositories;
using Craftsman.Domain.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Infra.Repositories;

public sealed class ProductionTaskRepository : IProductionTaskRepository
{
    private readonly IApplicationCache? cache;
    private readonly AppDbContext dbContext;

    public ProductionTaskRepository(AppDbContext dbContext, IApplicationCache? cache = null)
    {
        this.dbContext = dbContext;
        this.cache = cache;
    }

    public async Task<ProductionTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ProductionTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    public async Task<IReadOnlyCollection<ProductionTask>> ListRangeAsync(
        DateOnly plannedStartDate, 
        DateOnly plannedEndDate, 
        ProductionTaskStatus? status = null, 
        CancellationToken cancellationToken = default)
    {
        var key = $"production:list:{status?.ToString() ?? "all"}:{plannedStartDate.ToString("yyyyMMdd")}:{plannedEndDate.ToString("yyyyMMdd")}";

        if (cache is not null)
        {
            return await cache.GetOrCreateAsync(key, token => ListCoreAsync(status, plannedStartDate, plannedEndDate, token), cancellationToken: cancellationToken);
        }

        return await ListCoreAsync(status, plannedStartDate, plannedEndDate, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductionTask>> ListAsync(
        ProductionTaskStatus? status = null,
        DateOnly? plannedDate = null,
        CancellationToken cancellationToken = default)
    {
        var key = $"production:list:{status?.ToString() ?? "all"}:{plannedDate?.ToString("yyyyMMdd") ?? "all"}";

        if (cache is not null)
        {
            return await cache.GetOrCreateAsync(key, token => ListCoreAsync(status, plannedDate, null, token), cancellationToken: cancellationToken);
        }

        return await ListCoreAsync(status, plannedDate, null, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductionTask>> ListByOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.ProductionTasks
            .AsNoTracking()
            .Where(task => task.OrderId == orderId)
            .OrderBy(task => task.PlannedStartAt)
            .ToListAsync(cancellationToken);

        return entities.Select(ToModel).ToList().AsReadOnly();
    }

    public async Task AddAsync(ProductionTask productionTask, CancellationToken cancellationToken = default)
    {
        await dbContext.ProductionTasks.AddAsync(ToEntity(productionTask), cancellationToken);
        cache?.RemoveByPrefix("production:");
    }

    public async Task UpdateAsync(ProductionTask productionTask, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ProductionTasks.FirstOrDefaultAsync(
            existing => existing.Id == productionTask.Id,
            cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException($"Production task '{productionTask.Id}' was not found.");
        }
        else
        {
            MapToEntity(productionTask, entity);
        }

        cache?.RemoveByPrefix("production:");
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ProductionTasks.FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
        if (entity is null)
        {
            throw new InvalidOperationException($"Production task '{id}' was not found.");
        }

        dbContext.ProductionTasks.Remove(entity);
        cache?.RemoveByPrefix("production:");
    }

    public async Task DeleteByOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.ProductionTasks
            .Where(task => task.OrderId == orderId)
            .ToListAsync(cancellationToken);

        dbContext.ProductionTasks.RemoveRange(entities);
        cache?.RemoveByPrefix("production:");
    }

    private async Task<IReadOnlyCollection<ProductionTask>> ListCoreAsync(
        ProductionTaskStatus? status,
        DateOnly? plannedStartDate,
        DateOnly? plannedEndDate,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ProductionTasks.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(task => task.Status == status.ToString());
        }

        if (plannedStartDate is not null && plannedEndDate is null)
        {
            var start = plannedStartDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var end = plannedStartDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(task => task.PlannedStartAt < end && task.PlannedAt >= start);
        }

        if (plannedStartDate is not null && plannedEndDate is not null)
        {
            var start = plannedStartDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var end = plannedEndDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(task => task.PlannedStartAt < end && task.PlannedAt >= start);
        }

        var entities = await query.OrderBy(task => task.PlannedStartAt).ToListAsync(cancellationToken);

        return entities.Select(ToModel).ToList().AsReadOnly();
    }

    private static ProductionTask ToModel(ProductionTaskEntity entity)
    {
        return new ProductionTask(
            entity.Id,
            entity.OrderId,
            entity.OrderItemId,
            entity.ProductId,
            entity.Quantity,
            entity.ProductionDurationHours,
            Enum.Parse<ProductionTaskStatus>(entity.Status),
            entity.PlannedStartAt,
            entity.PlannedAt,
            entity.StartedAt,
            entity.CompletedAt,
            raisePlannedEvent: false);
    }

    private static ProductionTaskEntity ToEntity(ProductionTask productionTask)
    {
        var entity = new ProductionTaskEntity();
        MapToEntity(productionTask, entity);
        return entity;
    }

    private static void MapToEntity(ProductionTask productionTask, ProductionTaskEntity entity)
    {
        entity.Id = productionTask.Id;
        entity.OrderId = productionTask.OrderId;
        entity.OrderItemId = productionTask.OrderItemId;
        entity.ProductId = productionTask.ProductId;
        entity.Quantity = productionTask.Quantity;
        entity.ProductionDurationHours = productionTask.ProductionDurationHours;
        entity.Status = productionTask.Status.ToString();
        entity.PlannedStartAt = productionTask.PlannedStartAt;
        entity.PlannedAt = productionTask.PlannedAt;
        entity.StartedAt = productionTask.StartedAt;
        entity.CompletedAt = productionTask.CompletedAt;
    }
}
