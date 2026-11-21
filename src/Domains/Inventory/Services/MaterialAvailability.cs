namespace Craftsman.Domain.Inventory.Services;

public sealed record MaterialAvailability(
    Guid RawMaterialId,
    string Name,
    string UnitOfMeasure,
    decimal RequiredQuantity,
    decimal AvailableQuantity,
    bool IsConfigured)
{
    public bool IsAvailable => IsConfigured && AvailableQuantity >= RequiredQuantity;

    public decimal MissingQuantity => Math.Max(RequiredQuantity - AvailableQuantity, 0);
}
