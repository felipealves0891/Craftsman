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

    [Display(Name = "Nome do cliente")]
    public string? CustomerName { get; set; }

    [Display(Name = "Telefone")]
    public string? CustomerPhone { get; set; }

    [EmailAddress]
    [Display(Name = "Email")]
    public string? CustomerEmail { get; set; }

    [Display(Name = "Descricao do desconto")]
    public string? DiscountDescription { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Valor do desconto")]
    public decimal DiscountValue { get; set; }

    [Display(Name = "Desconto percentual")]
    public bool DiscountIsPercent { get; set; }

    public List<ManualOrderItemInputModel> Items { get; set; } =
    [
        new()
    ];
}

public sealed record CustomerOptionViewModel(
    Guid Id,
    string Name,
    string? Phone,
    string? Email);

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
