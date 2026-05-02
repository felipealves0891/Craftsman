using System.ComponentModel.DataAnnotations;
using Craftsman.Domain.ProductCatalog.Entities;

namespace Craftsman.App.Models;

public sealed record ProductListItemViewModel(
    Guid Id,
    string Name,
    string Status,
    int ProductionDurationHours,
    decimal HourlyRate,
    int BillOfMaterialsItems);

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

    [Range(1, int.MaxValue)]
    [Display(Name = "Tempo de producao (horas)")]
    public int ProductionDurationHours { get; set; } = 1;

    [Range(0, double.MaxValue)]
    [Display(Name = "Valor por hora")]
    public decimal HourlyRate { get; set; }
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
