using Craftsman.Domain.Repositories;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Infra.Repositories;

public abstract class EfRepository<TModel, TEntity, TId> : IRepository<TModel, TId>
    where TModel : class
    where TEntity : class, IPersistenceEntity<TId>
{
    private readonly AppDbContext dbContext;

    protected EfRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<TModel?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Set<TEntity>().FirstOrDefaultAsync(item => item.Id!.Equals(id), cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    public async Task AddAsync(TModel model, CancellationToken cancellationToken = default)
    {
        var entity = ToEntity(model);

        await dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(TModel model, CancellationToken cancellationToken = default)
    {
        var entity = ToEntity(model);
        var existing = await dbContext.Set<TEntity>()
            .FirstOrDefaultAsync(item => item.Id!.Equals(entity.Id), cancellationToken);

        if (existing is null)
        {
            throw new InvalidOperationException($"{typeof(TEntity).Name} with id '{entity.Id}' was not found.");
        }

        dbContext.Entry(existing).CurrentValues.SetValues(entity);
    }

    protected abstract TModel ToModel(TEntity entity);

    protected abstract TEntity ToEntity(TModel model);
}
