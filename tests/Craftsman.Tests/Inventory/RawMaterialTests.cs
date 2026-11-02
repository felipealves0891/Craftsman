using Craftsman.Domain.Inventory.Entities;

namespace Craftsman.Tests.Inventory;

public sealed class RawMaterialTests
{
    [Fact]
    public void Raw_material_can_be_created_without_low_stock_rule()
    {
        var rawMaterial = new RawMaterial(Guid.NewGuid(), "Tecido", "m");

        Assert.Null(rawMaterial.MinimumStockLevel);
        Assert.Null(rawMaterial.CriticalStockLevel);
        Assert.Equal(StockAlertLevel.Normal, rawMaterial.GetStockAlertLevel(0));
    }

    [Fact]
    public void Raw_material_accepts_minimum_and_critical_levels()
    {
        var rawMaterial = new RawMaterial(Guid.NewGuid(), "Tecido", "m", minimumStockLevel: 10, criticalStockLevel: 3);

        Assert.Equal(10, rawMaterial.MinimumStockLevel);
        Assert.Equal(3, rawMaterial.CriticalStockLevel);
    }

    [Fact]
    public void Raw_material_rejects_negative_low_stock_levels()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RawMaterial(Guid.NewGuid(), "Tecido", "m", minimumStockLevel: -1));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RawMaterial(Guid.NewGuid(), "Tecido", "m", criticalStockLevel: -1));
    }

    [Fact]
    public void Raw_material_rejects_critical_level_greater_than_minimum_level()
    {
        Assert.Throws<ArgumentException>(() =>
            new RawMaterial(Guid.NewGuid(), "Tecido", "m", minimumStockLevel: 5, criticalStockLevel: 6));
    }

    [Fact]
    public void Raw_material_validates_identity_and_required_text()
    {
        Assert.Throws<ArgumentException>(() => new RawMaterial(Guid.Empty, "Tecido", "m"));
        Assert.Throws<ArgumentException>(() => new RawMaterial(Guid.NewGuid(), " ", "m"));
        Assert.Throws<ArgumentException>(() => new RawMaterial(Guid.NewGuid(), "Tecido", "\t"));
    }

    [Fact]
    public void Raw_material_can_be_deactivated_and_activated_again()
    {
        var rawMaterial = new RawMaterial(Guid.NewGuid(), "  Tecido ", " m ");

        Assert.Equal("Tecido", rawMaterial.Name);
        Assert.Equal("m", rawMaterial.UnitOfMeasure);
        Assert.True(rawMaterial.CanBeConsumed);

        rawMaterial.Deactivate();
        Assert.False(rawMaterial.CanBeConsumed);

        rawMaterial.Activate();
        Assert.True(rawMaterial.CanBeConsumed);
    }

    [Fact]
    public void Raw_material_updates_low_stock_rules_and_classifies_warning_without_critical_level()
    {
        var rawMaterial = new RawMaterial(Guid.NewGuid(), "Tecido", "m");

        rawMaterial.SetLowStockRule(8, null);

        Assert.Equal(8, rawMaterial.MinimumStockLevel);
        Assert.Null(rawMaterial.CriticalStockLevel);
        Assert.Equal(StockAlertLevel.Warning, rawMaterial.GetStockAlertLevel(8));
        Assert.Equal(StockAlertLevel.Normal, rawMaterial.GetStockAlertLevel(9));
        Assert.Throws<ArgumentOutOfRangeException>(() => rawMaterial.SetLowStockRule(-1, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => rawMaterial.SetLowStockRule(null, -1));
    }

    [Theory]
    [InlineData(11, StockAlertLevel.Normal)]
    [InlineData(10, StockAlertLevel.Warning)]
    [InlineData(3, StockAlertLevel.Critical)]
    [InlineData(2, StockAlertLevel.Critical)]
    public void Raw_material_classifies_balance_by_low_stock_rule(decimal balance, StockAlertLevel expected)
    {
        var rawMaterial = new RawMaterial(Guid.NewGuid(), "Tecido", "m", minimumStockLevel: 10, criticalStockLevel: 3);

        Assert.Equal(expected, rawMaterial.GetStockAlertLevel(balance));
    }
}
