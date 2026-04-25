using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Infra.Repositories;

public sealed class ProductMappingRepository : IProductMappingRepository
{
    private readonly AppDbContext dbContext;

    public ProductMappingRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
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

    public async Task AddAsync(ProductMapping mapping, CancellationToken cancellationToken = default)
    {
        await dbContext.ProductMappings.AddAsync(ToEntity(mapping), cancellationToken);
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
