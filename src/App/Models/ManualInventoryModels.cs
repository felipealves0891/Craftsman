using System.ComponentModel.DataAnnotations;
using Craftsman.Domain.Inventory.Entities;

namespace Craftsman.App.Models;

public sealed record RawMaterialListItemViewModel(
    Guid Id,
    string Name,
    string UnitOfMeasure,
    string Status,
    decimal? MinimumStockLevel,
    decimal? CriticalStockLevel,
    StockAlertLevel? AlertLevel);

public sealed record RawMaterialOptionViewModel(Guid Id, string Name, string UnitOfMeasure);

public sealed record InventoryBalanceViewModel(
    Guid RawMaterialId,
    string RawMaterialName,
    string UnitOfMeasure,
    decimal Balance,
    StockAlertLevel AlertLevel,
    decimal? ReachedLimit);

public sealed record StockAlertViewModel(
    Guid RawMaterialId,
    string RawMaterialName,
    string UnitOfMeasure,
    decimal Balance,
    StockAlertLevel Level,
    decimal ReachedLimit)
{
    public string LevelLabel => Level switch
    {
        StockAlertLevel.Critical => "Critico",
        StockAlertLevel.Warning => "Aviso",
        _ => "Normal"
    };
}

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

public sealed class RawMaterialInputModel : IValidatableObject
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

    [Range(0, double.MaxValue)]
    [Display(Name = "Quantidade minima")]
    public decimal? MinimumStockLevel { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Quantidade critica")]
    public decimal? CriticalStockLevel { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinimumStockLevel.HasValue &&
            CriticalStockLevel.HasValue &&
            CriticalStockLevel.Value > MinimumStockLevel.Value)
        {
            yield return new ValidationResult(
                "Quantidade critica deve ser menor ou igual a quantidade minima.",
                [nameof(CriticalStockLevel)]);
        }
    }
}

public sealed class StockMovementInputModel : IValidatableObject
{
    [Required]
    [Display(Name = "Materia-prima")]
    public Guid RawMaterialId { get; set; }

    [Required]
    [Display(Name = "Tipo")]
    public StockMovementType Type { get; set; } = StockMovementType.Inbound;

    [Display(Name = "Quantidade")]
    public decimal Quantity { get; set; }

    [Display(Name = "Motivo")]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "Referencia")]
    public string? BusinessReference { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Custo unitario")]
    public decimal UnitCostAmount { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type == StockMovementType.Outbound && string.IsNullOrWhiteSpace(Reason))
        {
            yield return new ValidationResult(
                "Informe o motivo da saida manual.",
                [nameof(Reason)]);
        }
    }
}
