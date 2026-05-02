namespace Craftsman.Infra.Persistence.Entities;

public sealed class ProductEntity : IPersistenceEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int ProductionDurationHours { get; set; } = 1;

    public decimal HourlyRate { get; set; }

    public List<BillOfMaterialsItemEntity> BillOfMaterials { get; set; } = [];

    public List<ProductMappingEntity> Mappings { get; set; } = [];
}
