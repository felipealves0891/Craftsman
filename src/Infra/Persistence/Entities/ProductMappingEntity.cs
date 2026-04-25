namespace Craftsman.Infra.Persistence.Entities;

public sealed class ProductMappingEntity : IPersistenceEntity<Guid>
{
    public Guid Id { get; set; }

    public string Source { get; set; } = string.Empty;

    public string ExternalItemId { get; set; } = string.Empty;

    public Guid ProductId { get; set; }

    public ProductEntity? Product { get; set; }
}
