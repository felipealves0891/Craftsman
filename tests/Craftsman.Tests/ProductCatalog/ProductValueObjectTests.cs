using Craftsman.Domain.ProductCatalog.Entities;

namespace Craftsman.Tests.ProductCatalog;

public sealed class ProductValueObjectTests
{
    [Fact]
    public void Bill_of_material_item_requires_material_and_positive_quantity()
    {
        var materialId = Guid.NewGuid();
        var item = new BillOfMaterialsItem(materialId, 1.25m);

        Assert.Equal(materialId, item.RawMaterialId);
        Assert.Equal(1.25m, item.QuantityPerUnit);
        Assert.Throws<ArgumentException>(() => new BillOfMaterialsItem(Guid.Empty, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BillOfMaterialsItem(materialId, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BillOfMaterialsItem(materialId, -1));
    }

    [Fact]
    public void Product_mapping_trims_values_and_requires_each_identifier()
    {
        var id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var mapping = new ProductMapping(id, " Shopee ", " SKU-42 ", productId);

        Assert.Equal("Shopee", mapping.Source);
        Assert.Equal("SKU-42", mapping.ExternalItemId);
        Assert.Equal(productId, mapping.ProductId);
        Assert.Throws<ArgumentException>(() => new ProductMapping(Guid.Empty, "Shopee", "SKU", productId));
        Assert.Throws<ArgumentException>(() => new ProductMapping(id, " ", "SKU", productId));
        Assert.Throws<ArgumentException>(() => new ProductMapping(id, "Shopee", "\t", productId));
        Assert.Throws<ArgumentException>(() => new ProductMapping(id, "Shopee", "SKU", Guid.Empty));
    }
}
