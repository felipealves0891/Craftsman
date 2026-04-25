using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Production.Repositories;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Infra.Repositories;

public sealed class ProductionTaskRepository : IProductionTaskRepository
{
    private readonly AppDbContext dbContext;

    public ProductionTaskRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<ProductionTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ProductionTasks.FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    public async Task AddAsync(ProductionTask productionTask, CancellationToken cancellationToken = default)
    {
        await dbContext.ProductionTasks.AddAsync(ToEntity(productionTask), cancellationToken);
    }

    public Task UpdateAsync(ProductionTask productionTask, CancellationToken cancellationToken = default)
    {
        dbContext.ProductionTasks.Update(ToEntity(productionTask));
        return Task.CompletedTask;
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
        return new ProductionTaskEntity
        {
            Id = productionTask.Id,
            OrderId = productionTask.OrderId,
            OrderItemId = productionTask.OrderItemId,
            ProductId = productionTask.ProductId,
            Quantity = productionTask.Quantity,
            Status = productionTask.Status.ToString(),
            PlannedAt = productionTask.PlannedAt,
            StartedAt = productionTask.StartedAt,
            CompletedAt = productionTask.CompletedAt
        };
    }
}
