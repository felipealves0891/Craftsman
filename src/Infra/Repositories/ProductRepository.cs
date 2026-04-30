using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Infra.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly IApplicationCache? cache;
    private readonly AppDbContext dbContext;

    public ProductRepository(AppDbContext dbContext, IApplicationCache? cache = null)
    {
        this.dbContext = dbContext;
        this.cache = cache;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Products
            .Include(product => product.BillOfMaterials)
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    public async Task<IReadOnlyCollection<Product>> ListAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = "product-catalog:products";
        if (cache is not null)
        {
            return await cache.GetOrCreateAsync(cacheKey, LoadProductsAsync, cancellationToken: cancellationToken);
        }

        return await LoadProductsAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<Product>> LoadProductsAsync(CancellationToken cancellationToken)
    {
        var entities = await dbContext.Products
            .Include(product => product.BillOfMaterials)
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToModel).ToList().AsReadOnly();
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await dbContext.Products.AddAsync(ToEntity(product), cancellationToken);
        cache?.RemoveByPrefix("product-catalog:");
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Products
            .Include(existing => existing.BillOfMaterials)
            .FirstOrDefaultAsync(existing => existing.Id == product.Id, cancellationToken);

        if (entity is null)
        {
            dbContext.Products.Update(ToEntity(product));
        }
        else
        {
            entity.Name = product.Name;
            entity.Status = product.Status.ToString();
            entity.ProductionDurationDays = product.ProductionDurationDays;
            entity.Barcode = product.Barcode;
            entity.BillOfMaterials.Clear();
            foreach (var item in product.BillOfMaterials)
            {
                entity.BillOfMaterials.Add(new BillOfMaterialsItemEntity
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    RawMaterialId = item.RawMaterialId,
                    QuantityPerUnit = item.QuantityPerUnit
                });
            }
        }

        cache?.RemoveByPrefix("product-catalog:");
    }

    private static Product ToModel(ProductEntity entity)
    {
        var billOfMaterials = entity.BillOfMaterials.Select(item => new BillOfMaterialsItem(
            item.RawMaterialId,
            item.QuantityPerUnit));

        return new Product(
            entity.Id,
            entity.Name,
            Enum.Parse<ProductStatus>(entity.Status),
            entity.ProductionDurationDays,
            entity.Barcode,
            billOfMaterials);
    }

    private static ProductEntity ToEntity(Product product)
    {
        return new ProductEntity
        {
            Id = product.Id,
            Name = product.Name,
            Status = product.Status.ToString(),
            ProductionDurationDays = product.ProductionDurationDays,
            Barcode = product.Barcode,
            BillOfMaterials = product.BillOfMaterials.Select(item => new BillOfMaterialsItemEntity
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                RawMaterialId = item.RawMaterialId,
                QuantityPerUnit = item.QuantityPerUnit
            }).ToList()
        };
    }
}
