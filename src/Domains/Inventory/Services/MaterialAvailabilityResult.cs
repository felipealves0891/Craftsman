namespace Craftsman.Domain.Inventory.Services;

public sealed class MaterialAvailabilityResult
{
    public bool IsAvailable => MissingMaterials.Count == 0;

    public IReadOnlyCollection<MissingMaterial> MissingMaterials { get; }

    public MaterialAvailabilityResult(IEnumerable<MissingMaterial> missingMaterials)
    {
        MissingMaterials = missingMaterials.ToList().AsReadOnly();
    }

    public static MaterialAvailabilityResult Available() => new([]);
}
