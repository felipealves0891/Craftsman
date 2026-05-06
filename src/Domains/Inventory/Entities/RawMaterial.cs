namespace Craftsman.Domain.Inventory.Entities;

public sealed class RawMaterial
{
    public Guid Id { get; }

    public string Name { get; private set; }

    public string UnitOfMeasure { get; private set; }

    public RawMaterialStatus Status { get; private set; }

    public decimal? MinimumStockLevel { get; private set; }

    public decimal? CriticalStockLevel { get; private set; }

    public bool CanBeConsumed => Status == RawMaterialStatus.Active;

    public RawMaterial(
        Guid id,
        string name,
        string unitOfMeasure,
        RawMaterialStatus status = RawMaterialStatus.Active,
        decimal? minimumStockLevel = null,
        decimal? criticalStockLevel = null)
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
        SetLowStockRule(minimumStockLevel, criticalStockLevel);
    }

    public void Activate()
    {
        Status = RawMaterialStatus.Active;
    }

    public void Deactivate()
    {
        Status = RawMaterialStatus.Inactive;
    }

    public void SetLowStockRule(decimal? minimumStockLevel, decimal? criticalStockLevel)
    {
        if (minimumStockLevel < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumStockLevel), "Minimum stock level must be zero or greater.");
        }

        if (criticalStockLevel < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(criticalStockLevel), "Critical stock level must be zero or greater.");
        }

        if (minimumStockLevel.HasValue &&
            criticalStockLevel.HasValue &&
            criticalStockLevel.Value > minimumStockLevel.Value)
        {
            throw new ArgumentException("Critical stock level must be less than or equal to minimum stock level.", nameof(criticalStockLevel));
        }

        MinimumStockLevel = minimumStockLevel;
        CriticalStockLevel = criticalStockLevel;
    }

    public StockAlertLevel GetStockAlertLevel(decimal currentBalance)
    {
        if (CriticalStockLevel.HasValue && currentBalance <= CriticalStockLevel.Value)
        {
            return StockAlertLevel.Critical;
        }

        if (MinimumStockLevel.HasValue && currentBalance <= MinimumStockLevel.Value)
        {
            return StockAlertLevel.Warning;
        }

        return StockAlertLevel.Normal;
    }
}
