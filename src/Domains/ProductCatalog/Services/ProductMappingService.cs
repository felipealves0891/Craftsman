using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.ProductCatalog.Repositories;

namespace Craftsman.Domain.ProductCatalog.Services;

public sealed class ProductMappingService
{
    private readonly IProductMappingRepository productMappingRepository;

    public ProductMappingService(IProductMappingRepository productMappingRepository)
    {
        this.productMappingRepository = productMappingRepository;
    }

    public async Task<ProductMapping> CreateMappingAsync(
        string source,
        string externalItemId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var existingMapping = await productMappingRepository.GetByExternalItemAsync(source, externalItemId, cancellationToken);

        if (existingMapping is not null)
        {
            throw new InvalidOperationException("External item is already mapped to an internal product.");
        }

        var mapping = new ProductMapping(Guid.NewGuid(), source, externalItemId, productId);
        await productMappingRepository.AddAsync(mapping, cancellationToken);

        return mapping;
    }
}
