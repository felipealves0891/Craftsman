namespace Craftsman.Domain.ProductCatalog.Entities;

public sealed class BillOfMaterialsItem
{
    public Guid RawMaterialId { get; }

    public decimal QuantityPerUnit { get; }

    public BillOfMaterialsItem(Guid rawMaterialId, decimal quantityPerUnit)
    {
        if (rawMaterialId == Guid.Empty)
        {
            throw new ArgumentException("Raw material id is required.", nameof(rawMaterialId));
        }

        if (quantityPerUnit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityPerUnit), "Quantity per unit must be greater than zero.");
        }

        RawMaterialId = rawMaterialId;
        QuantityPerUnit = quantityPerUnit;
    }
}
