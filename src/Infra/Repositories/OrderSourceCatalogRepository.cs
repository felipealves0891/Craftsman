using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.Repositories;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Infra.Repositories;

public sealed class OrderSourceCatalogRepository : IOrderSourceCatalogRepository
{
    private readonly AppDbContext dbContext;

    public OrderSourceCatalogRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<OrderSource?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        var entity = await dbContext.OrderSources
            .FirstOrDefaultAsync(source => source.Name == normalizedName, cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    public async Task<IReadOnlyCollection<OrderSource>> ListAsync(CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.OrderSources
            .OrderBy(source => source.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToModel).ToList().AsReadOnly();
    }

    public async Task AddAsync(OrderSource source, CancellationToken cancellationToken = default)
    {
        await dbContext.OrderSources.AddAsync(ToEntity(source), cancellationToken);
    }

    private static OrderSource ToModel(OrderSourceEntity entity)
    {
        return new OrderSource(entity.Id, entity.Name, entity.CreatedAt);
    }

    private static OrderSourceEntity ToEntity(OrderSource source)
    {
        return new OrderSourceEntity
        {
            Id = source.Id,
            Name = source.Name,
            CreatedAt = source.CreatedAt
        };
    }
}
