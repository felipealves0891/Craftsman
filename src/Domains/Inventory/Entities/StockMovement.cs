namespace Craftsman.Domain.Inventory.Entities;

public sealed class StockMovement
{
    public Guid Id { get; }

    public Guid RawMaterialId { get; }

    public StockMovementType Type { get; }

    public decimal Quantity { get; }

    public string Reason { get; }

    public string? BusinessReference { get; }

    public DateTimeOffset OccurredAt { get; }

    public decimal UnitCostAmount { get; }

    public decimal TotalCostAmount => Quantity * UnitCostAmount;

    public decimal SignedQuantity => Type switch
    {
        StockMovementType.Inbound => Quantity,
        StockMovementType.Outbound => -Quantity,
        StockMovementType.Adjustment => Quantity,
        _ => throw new InvalidOperationException($"Unsupported stock movement type {Type}.")
    };

    public StockMovement(
        Guid id,
        Guid rawMaterialId,
        StockMovementType type,
        decimal quantity,
        string reason,
        string? businessReference,
        DateTimeOffset? occurredAt = null,
        decimal unitCostAmount = 0)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Stock movement id is required.", nameof(id));
        }

        if (rawMaterialId == Guid.Empty)
        {
            throw new ArgumentException("Raw material id is required.", nameof(rawMaterialId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required.", nameof(reason));
        }

        if (unitCostAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitCostAmount), "Unit cost cannot be negative.");
        }

        Id = id;
        RawMaterialId = rawMaterialId;
        Type = type;
        Quantity = quantity;
        Reason = reason.Trim();
        BusinessReference = string.IsNullOrWhiteSpace(businessReference) ? null : businessReference.Trim();
        OccurredAt = occurredAt ?? DateTimeOffset.UtcNow;
        UnitCostAmount = unitCostAmount;
    }
}
