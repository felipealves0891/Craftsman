namespace Craftsman.Domain.Inventory.Services;

public sealed record MissingMaterial(Guid RawMaterialId, decimal RequiredQuantity, decimal AvailableQuantity)
{
    public decimal MissingQuantity => RequiredQuantity - AvailableQuantity;
}
