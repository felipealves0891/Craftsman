namespace Craftsman.Domain.ProductCatalog.Entities;

public sealed class Product
{
    private readonly List<BillOfMaterialsItem> billOfMaterials;

    public Guid Id { get; }

    public string Name { get; private set; }

    public ProductStatus Status { get; private set; }

    public IReadOnlyCollection<BillOfMaterialsItem> BillOfMaterials => billOfMaterials.AsReadOnly();

    public bool CanBePlannedForProduction => Status == ProductStatus.Active && billOfMaterials.Count > 0;

    public Product(
        Guid id,
        string name,
        ProductStatus status = ProductStatus.Active,
        IEnumerable<BillOfMaterialsItem>? billOfMaterials = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Product id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        Id = id;
        Name = name.Trim();
        Status = status;
        this.billOfMaterials = billOfMaterials?.ToList() ?? [];
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        Name = name.Trim();
    }

    public void Activate()
    {
        Status = ProductStatus.Active;
    }

    public void Deactivate()
    {
        Status = ProductStatus.Inactive;
    }

    public void ReplaceBillOfMaterials(IEnumerable<BillOfMaterialsItem> items)
    {
        var materialItems = items?.ToList() ?? throw new ArgumentNullException(nameof(items));

        if (materialItems.Count == 0)
        {
            throw new ArgumentException("Bill of materials must contain at least one item.", nameof(items));
        }

        if (materialItems.Select(item => item.RawMaterialId).Distinct().Count() != materialItems.Count)
        {
            throw new InvalidOperationException("Bill of materials cannot contain duplicate raw materials.");
        }

        billOfMaterials.Clear();
        billOfMaterials.AddRange(materialItems);
    }
}
