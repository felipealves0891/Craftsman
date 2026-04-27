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
        var entity = await dbContext.ProductionTasks.FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    public async Task<IReadOnlyCollection<ProductionTask>> ListAsync(
        ProductionTaskStatus? status = null,
        DateOnly? plannedDate = null,
        CancellationToken cancellationToken = default)
    {
        var key = $"production:list:{status?.ToString() ?? "all"}:{plannedDate?.ToString("yyyyMMdd") ?? "all"}";

        if (cache is not null)
        {
            return await cache.GetOrCreateAsync(key, token => ListCoreAsync(status, plannedDate, token), cancellationToken: cancellationToken);
        }

        return await ListCoreAsync(status, plannedDate, cancellationToken);
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
            dbContext.ProductionTasks.Update(ToEntity(productionTask));
        }
        else
        {
            MapToEntity(productionTask, entity);
        }

        cache?.RemoveByPrefix("production:");
    }

    private async Task<IReadOnlyCollection<ProductionTask>> ListCoreAsync(
        ProductionTaskStatus? status,
        DateOnly? plannedDate,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ProductionTasks.AsQueryable();

        if (status is not null)
        {
            query = query.Where(task => task.Status == status.ToString());
        }

        if (plannedDate is not null)
        {
            var start = plannedDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var end = plannedDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(task => task.PlannedAt >= start && task.PlannedAt < end);
        }

        var entities = await query.OrderByDescending(task => task.PlannedAt).ToListAsync(cancellationToken);

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
            Enum.Parse<ProductionTaskStatus>(entity.Status),
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
        entity.Status = productionTask.Status.ToString();
        entity.PlannedAt = productionTask.PlannedAt;
        entity.StartedAt = productionTask.StartedAt;
        entity.CompletedAt = productionTask.CompletedAt;
    }
}
