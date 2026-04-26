using System.ComponentModel.DataAnnotations;

namespace Craftsman.App.Models;

public sealed class ManualOrderInputModel
{
    [Display(Name = "Referência manual")]
    public string? Reference { get; set; }

    [Required]
    [Display(Name = "Cliente")]
    public string CustomerName { get; set; } = string.Empty;

    [EmailAddress]
    [Display(Name = "E-mail")]
    public string? CustomerEmail { get; set; }

    public List<ManualOrderItemInputModel> Items { get; set; } =
    [
        new()
    ];
}

public sealed class ManualOrderItemInputModel
{
    [Display(Name = "Código externo")]
    public string? ExternalItemId { get; set; }

    [Display(Name = "Descrição")]
    public string Description { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    [Display(Name = "Quantidade")]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Valor unitário")]
    public decimal UnitPriceAmount { get; set; }

    [Display(Name = "Produto interno")]
    public Guid? ProductId { get; set; }
}
