using Craftsman.Domain.Finance.Entities;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Shipping.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Craftsman.App.Presentation;

public static class EnumPresentation
{
    public static string Label<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        return LabelCore(value);
    }

    public static string Label(Enum value)
    {
        return LabelCore(value);
    }

    private static string LabelCore(object value)
    {
        return value switch
        {
            FinancialSettlementStatus.Calculated => "Calculado",
            FinancialSettlementStatus.Failed => "Falhou",

            ProductStatus.Active => "Ativo",
            ProductStatus.Inactive => "Inativo",

            ProductionTaskStatus.Planned => "Planejada",
            ProductionTaskStatus.InProduction => "Em producao",
            ProductionTaskStatus.Completed => "Concluida",
            ProductionTaskStatus.Cancelled => "Cancelada",

            RawMaterialStatus.Active => "Ativo",
            RawMaterialStatus.Inactive => "Inativo",

            ShipmentStatus.Created => "Criado",
            ShipmentStatus.InTransit => "Em transito",
            ShipmentStatus.DeliveryAttempted => "Tentativa de entrega",
            ShipmentStatus.Delivered => "Entregue",
            ShipmentStatus.Cancelled => "Cancelado",

            StockAlertLevel.Normal => "Normal",
            StockAlertLevel.Warning => "Aviso",
            StockAlertLevel.Critical => "Critico",

            StockMovementType.Inbound => "Entrada",
            StockMovementType.Outbound => "Saida",
            StockMovementType.Adjustment => "Ajuste",

            OrderStatus.Normalized => "Normalizado",
            OrderStatus.ReadyForProduction => "Pronto para producao",
            OrderStatus.InProduction => "Em producao",
            OrderStatus.Shipped => "Enviado",
            OrderStatus.Delivered => "Entregue",
            OrderStatus.Cancelled => "Cancelado",

            _ => value.ToString() ?? "-"
        };
    }

    public static string Label(Type enumType, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !enumType.IsEnum)
        {
            return value ?? "-";
        }

        return Enum.TryParse(enumType, value, ignoreCase: false, out var parsed)
            ? Label((Enum)parsed)
            : value;
    }

    public static IReadOnlyCollection<SelectListItem> SelectList<TEnum>(TEnum? selectedValue = null)
        where TEnum : struct, Enum
    {
        return Enum.GetValues<TEnum>()
            .Select(value => new SelectListItem
            {
                Value = value.ToString(),
                Text = Label(value),
                Selected = selectedValue.HasValue && EqualityComparer<TEnum>.Default.Equals(value, selectedValue.Value)
            })
            .ToArray();
    }
}
