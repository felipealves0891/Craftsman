namespace Craftsman.Domain.Inventory.Services;

public sealed record MaterialRequirement(Guid RawMaterialId, decimal RequiredQuantity);
