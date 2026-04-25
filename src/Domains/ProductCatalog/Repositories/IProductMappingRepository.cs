using Craftsman.Domain.ProductCatalog.Entities;

namespace Craftsman.Domain.ProductCatalog.Repositories;

public interface IProductMappingRepository
{
    Task<ProductMapping?> GetByExternalItemAsync(string source, string externalItemId, CancellationToken cancellationToken = default);

    Task AddAsync(ProductMapping mapping, CancellationToken cancellationToken = default);
}
