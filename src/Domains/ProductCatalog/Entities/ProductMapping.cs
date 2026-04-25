namespace Craftsman.Domain.ProductCatalog.Entities;

public sealed class ProductMapping
{
    public Guid Id { get; }

    public string Source { get; }

    public string ExternalItemId { get; }

    public Guid ProductId { get; }

    public ProductMapping(Guid id, string source, string externalItemId, Guid productId)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Mapping id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source is required.", nameof(source));
        }

        if (string.IsNullOrWhiteSpace(externalItemId))
        {
            throw new ArgumentException("External item id is required.", nameof(externalItemId));
        }

        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.", nameof(productId));
        }

        Id = id;
        Source = source.Trim();
        ExternalItemId = externalItemId.Trim();
        ProductId = productId;
    }
}
