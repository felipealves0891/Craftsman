namespace Craftsman.Infra.Persistence.Entities;

public sealed class RawMaterialEntity : IPersistenceEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string UnitOfMeasure { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}
