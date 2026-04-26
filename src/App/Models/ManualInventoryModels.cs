using System.ComponentModel.DataAnnotations;
using Craftsman.Domain.Inventory.Entities;

namespace Craftsman.App.Models;

public sealed record RawMaterialListItemViewModel(Guid Id, string Name, string UnitOfMeasure, string Status);

public sealed record RawMaterialOptionViewModel(Guid Id, string Name, string UnitOfMeasure);

public sealed record InventoryBalanceViewModel(Guid RawMaterialId, string RawMaterialName, string UnitOfMeasure, decimal Balance);

public sealed record StockMovementViewModel(
    Guid Id,
    Guid RawMaterialId,
    string RawMaterialName,
    string Type,
    decimal Quantity,
    decimal UnitCostAmount,
    decimal TotalCostAmount,
    string Reason,
    string? BusinessReference,
    DateTimeOffset OccurredAt);

public sealed class RawMaterialInputModel
{
    public Guid? Id { get; set; }

    [Required]
    [Display(Name = "Nome")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Unidade de medida")]
    public string UnitOfMeasure { get; set; } = string.Empty;

    [Required]
    public RawMaterialStatus Status { get; set; } = RawMaterialStatus.Active;
}

public sealed class StockMovementInputModel
{
    [Required]
    [Display(Name = "Materia-prima")]
    public Guid RawMaterialId { get; set; }

    [Required]
    [Display(Name = "Tipo")]
    public StockMovementType Type { get; set; } = StockMovementType.Inbound;

    [Display(Name = "Quantidade")]
    public decimal Quantity { get; set; }

    [Required]
    [Display(Name = "Motivo")]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "Referencia")]
    public string? BusinessReference { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Custo unitario")]
    public decimal UnitCostAmount { get; set; }
}
