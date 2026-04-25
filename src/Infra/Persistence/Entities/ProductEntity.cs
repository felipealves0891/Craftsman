namespace Craftsman.Infra.Persistence.Entities;

public sealed class ProductEntity : IPersistenceEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public List<BillOfMaterialsItemEntity> BillOfMaterials { get; set; } = [];

    public List<ProductMappingEntity> Mappings { get; set; } = [];
}
