namespace Craftsman.Infra.Persistence.Entities;

public sealed class StockMovementEntity : IPersistenceEntity<Guid>
{
    public Guid Id { get; set; }

    public Guid RawMaterialId { get; set; }

    public RawMaterialEntity? RawMaterial { get; set; }

    public string Type { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitCostAmount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string? BusinessReference { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
}
