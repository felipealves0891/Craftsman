using System.ComponentModel.DataAnnotations;

namespace Craftsman.App.Models;

public sealed class ManualOrderInputModel
{
    public Guid? Id { get; set; }

    [Required]
    [Display(Name = "Origem")]
    public string Source { get; set; } = string.Empty;

    [Display(Name = "Referencia manual")]
    public string? Reference { get; set; }

    [Required]
    [Display(Name = "Data de envio")]
    public DateOnly? ShippingDate { get; set; }

    public List<ManualOrderItemInputModel> Items { get; set; } =
    [
        new()
    ];
}

public sealed class OrderSourceInputModel
{
    [Required]
    [Display(Name = "Nova origem")]
    public string Name { get; set; } = string.Empty;
}

public sealed class ManualOrderItemInputModel
{
    [Display(Name = "Codigo externo")]
    public string? ExternalItemId { get; set; }

    [Display(Name = "Descricao")]
    public string Description { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    [Display(Name = "Quantidade")]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Valor unitario")]
    public decimal UnitPriceAmount { get; set; }

    [Display(Name = "Produto interno")]
    public Guid? ProductId { get; set; }
}
