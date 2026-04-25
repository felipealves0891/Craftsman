namespace Craftsman.Infra.Persistence.Entities;

public sealed class BillOfMaterialsItemEntity : IPersistenceEntity<Guid>
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public ProductEntity? Product { get; set; }

    public Guid RawMaterialId { get; set; }

    public decimal QuantityPerUnit { get; set; }
}
