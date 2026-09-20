using Craftsman.App.Presentation;
using Craftsman.Domain.Finance.Entities;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Shipping.Entities;

namespace Craftsman.Tests.Application;

public sealed class EnumPresentationTests
{
    [Theory]
    [InlineData(ProductStatus.Active, "Ativo")]
    [InlineData(ProductStatus.Inactive, "Inativo")]
    [InlineData(RawMaterialStatus.Active, "Ativo")]
    [InlineData(RawMaterialStatus.Inactive, "Inativo")]
    [InlineData(StockMovementType.Inbound, "Entrada")]
    [InlineData(StockMovementType.Outbound, "Saida")]
    [InlineData(StockMovementType.Adjustment, "Ajuste")]
    [InlineData(StockAlertLevel.Warning, "Aviso")]
    [InlineData(StockAlertLevel.Critical, "Critico")]
    [InlineData(OrderStatus.ReadyForProduction, "Pronto para producao")]
    [InlineData(OrderStatus.InProduction, "Em producao")]
    [InlineData(ProductionTaskStatus.Planned, "Planejada")]
    [InlineData(ProductionTaskStatus.Completed, "Concluida")]
    [InlineData(ShipmentStatus.InTransit, "Em transito")]
    [InlineData(ShipmentStatus.DeliveryAttempted, "Tentativa de entrega")]
    [InlineData(FinancialSettlementStatus.Calculated, "Calculado")]
    public void Domain_enum_labels_are_presented_in_brazilian_portuguese(Enum value, string expectedLabel)
    {
        Assert.Equal(expectedLabel, EnumPresentation.Label(value));
    }

    [Fact]
    public void Select_list_preserves_enum_values_and_uses_translated_text()
    {
        var options = EnumPresentation.SelectList<StockMovementType>(StockMovementType.Outbound);

        Assert.Contains(options, option => option.Value == nameof(StockMovementType.Inbound) && option.Text == "Entrada");
        Assert.Contains(options, option => option.Value == nameof(StockMovementType.Outbound) && option.Text == "Saida" && option.Selected);
        Assert.Contains(options, option => option.Value == nameof(StockMovementType.Adjustment) && option.Text == "Ajuste");
    }

    [Fact]
    public void String_values_are_translated_when_the_enum_type_is_known()
    {
        Assert.Equal("Entregue", EnumPresentation.Label(typeof(ShipmentStatus), nameof(ShipmentStatus.Delivered)));
    }
}
