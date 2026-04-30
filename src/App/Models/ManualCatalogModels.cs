using System.ComponentModel.DataAnnotations;
using Craftsman.Domain.ProductCatalog.Entities;

namespace Craftsman.App.Models;

public sealed record ProductListItemViewModel(Guid Id, string Name, string Status, int ProductionDurationDays, int BillOfMaterialsItems, string? Barcode);

public sealed record ProductOptionViewModel(Guid Id, string Name);

public sealed record ProductMappingListItemViewModel(
    Guid Id,
    string Source,
    string ExternalItemId,
    Guid ProductId,
    string ProductName);

public sealed class ProductInputModel
{
    public Guid? Id { get; set; }

    [Required]
    [Display(Name = "Nome")]
    public string Name { get; set; } = string.Empty;

    [Required]
    public ProductStatus Status { get; set; } = ProductStatus.Active;

    [Range(1, 365)]
    [Display(Name = "Tempo de produção (dias)")]
    public int ProductionDurationDays { get; set; } = 1;

    [StringLength(128)]
    [Display(Name = "Codigo de barras")]
    public string? Barcode { get; set; }
}

public sealed class BillOfMaterialsInputModel
{
    public Guid ProductId { get; set; }

    public List<BillOfMaterialsItemInputModel> Items { get; set; } =
    [
        new(),
        new(),
        new(),
        new(),
        new()
    ];
}

public sealed class BillOfMaterialsItemInputModel
{
    [Display(Name = "Materia-prima")]
    public Guid? RawMaterialId { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Quantidade por unidade")]
    public decimal QuantityPerUnit { get; set; }
}

public sealed class ProductMappingInputModel
{
    [Required]
    [Display(Name = "Origem")]
    public string Source { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Item externo")]
    public string ExternalItemId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Produto interno")]
    public Guid ProductId { get; set; }
}
