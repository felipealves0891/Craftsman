namespace Craftsman.Domain.Inventory.Entities;

public sealed class RawMaterial
{
    public Guid Id { get; }

    public string Name { get; private set; }

    public string UnitOfMeasure { get; private set; }

    public RawMaterialStatus Status { get; private set; }

    public bool CanBeConsumed => Status == RawMaterialStatus.Active;

    public RawMaterial(Guid id, string name, string unitOfMeasure, RawMaterialStatus status = RawMaterialStatus.Active)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Raw material id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Raw material name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(unitOfMeasure))
        {
            throw new ArgumentException("Unit of measure is required.", nameof(unitOfMeasure));
        }

        Id = id;
        Name = name.Trim();
        UnitOfMeasure = unitOfMeasure.Trim();
        Status = status;
    }

    public void Activate()
    {
        Status = RawMaterialStatus.Active;
    }

    public void Deactivate()
    {
        Status = RawMaterialStatus.Inactive;
    }
}
