using Craftsman.Domain.ProductCatalog.Entities;

namespace Craftsman.Tests.ProductCatalog;

public sealed class ProductTests
{
    [Fact]
    public void Product_can_be_planned_only_when_active_with_bill_of_materials()
    {
        var product = new Product(Guid.NewGuid(), "Caneca");

        Assert.False(product.CanBePlannedForProduction);

        product.ReplaceBillOfMaterials([new BillOfMaterialsItem(Guid.NewGuid(), 1.5m)]);

        Assert.True(product.CanBePlannedForProduction);

        product.Deactivate();

        Assert.False(product.CanBePlannedForProduction);
    }

    [Fact]
    public void Bill_of_materials_rejects_duplicate_raw_materials()
    {
        var rawMaterialId = Guid.NewGuid();
        var product = new Product(Guid.NewGuid(), "Caneca");

        Assert.Throws<InvalidOperationException>(() => product.ReplaceBillOfMaterials(
        [
            new BillOfMaterialsItem(rawMaterialId, 1),
            new BillOfMaterialsItem(rawMaterialId, 2)
        ]));
    }

    [Fact]
    public void Product_production_duration_must_be_at_least_one_day()
    {
        var product = new Product(Guid.NewGuid(), "Caneca");

        Assert.Equal(1, product.ProductionDurationDays);

        product.SetProductionDuration(3);

        Assert.Equal(3, product.ProductionDurationDays);
        Assert.Throws<ArgumentOutOfRangeException>(() => product.SetProductionDuration(0));
    }
}
