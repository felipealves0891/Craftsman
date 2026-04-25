using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Infra.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext dbContext;

    public ProductRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Products
            .Include(product => product.BillOfMaterials)
            .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await dbContext.Products.AddAsync(ToEntity(product), cancellationToken);
    }

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        dbContext.Products.Update(ToEntity(product));
        return Task.CompletedTask;
    }

    private static Product ToModel(ProductEntity entity)
    {
        var billOfMaterials = entity.BillOfMaterials.Select(item => new BillOfMaterialsItem(
            item.RawMaterialId,
            item.QuantityPerUnit));

        return new Product(entity.Id, entity.Name, Enum.Parse<ProductStatus>(entity.Status), billOfMaterials);
    }

    private static ProductEntity ToEntity(Product product)
    {
        return new ProductEntity
        {
            Id = product.Id,
            Name = product.Name,
            Status = product.Status.ToString(),
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
