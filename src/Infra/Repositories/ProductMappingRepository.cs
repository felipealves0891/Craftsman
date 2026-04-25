using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Infra.Repositories;

public sealed class ProductMappingRepository : IProductMappingRepository
{
    private readonly IApplicationCache? cache;
    private readonly AppDbContext dbContext;

    public ProductMappingRepository(AppDbContext dbContext, IApplicationCache? cache = null)
    {
        this.dbContext = dbContext;
        this.cache = cache;
    }

    public async Task<ProductMapping?> GetByExternalItemAsync(
        string source,
        string externalItemId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ProductMappings.FirstOrDefaultAsync(
            mapping => mapping.Source == source && mapping.ExternalItemId == externalItemId,
            cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    public async Task<IReadOnlyCollection<ProductMapping>> ListAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = "product-catalog:mappings";
        if (cache is not null)
        {
            return await cache.GetOrCreateAsync(cacheKey, LoadMappingsAsync, cancellationToken: cancellationToken);
        }

        return await LoadMappingsAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<ProductMapping>> LoadMappingsAsync(CancellationToken cancellationToken)
    {
        var entities = await dbContext.ProductMappings
            .OrderBy(mapping => mapping.Source)
            .ThenBy(mapping => mapping.ExternalItemId)
            .ToListAsync(cancellationToken);

        return entities.Select(ToModel).ToList().AsReadOnly();
    }

    public async Task AddAsync(ProductMapping mapping, CancellationToken cancellationToken = default)
    {
        await dbContext.ProductMappings.AddAsync(ToEntity(mapping), cancellationToken);
        cache?.RemoveByPrefix("product-catalog:");
    }

    private static ProductMapping ToModel(ProductMappingEntity entity)
    {
        return new ProductMapping(entity.Id, entity.Source, entity.ExternalItemId, entity.ProductId);
    }

    private static ProductMappingEntity ToEntity(ProductMapping mapping)
    {
        return new ProductMappingEntity
        {
            Id = mapping.Id,
            Source = mapping.Source,
            ExternalItemId = mapping.ExternalItemId,
            ProductId = mapping.ProductId
        };
    }
}
