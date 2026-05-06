using Craftsman.Domain.Inventory.Entities;

namespace Craftsman.Tests.Inventory;

public sealed class LowStockRazorViewTests
{
    [Fact]
    public void Material_form_contains_low_stock_fields()
    {
        var view = ReadView("src/App/Views/Inventory/EditMaterial.cshtml");

        Assert.Contains("asp-for=\"MinimumStockLevel\"", view);
        Assert.Contains("asp-for=\"CriticalStockLevel\"", view);
    }

    [Fact]
    public void Balances_view_displays_warning_and_critical_states()
    {
        var view = ReadView("src/App/Views/Inventory/Balances.cshtml");

        Assert.Contains("StockAlertLevel.Critical", view);
        Assert.Contains("StockAlertLevel.Warning", view);
        Assert.Contains("Critico", view);
        Assert.Contains("Aviso", view);
    }

    [Fact]
    public void Layout_contains_stock_alert_notification_component()
    {
        var layout = ReadView("src/App/Views/Shared/_Layout.cshtml");
        var componentView = ReadView("src/App/Views/Shared/Components/StockAlerts/Default.cshtml");

        Assert.Contains("Component.InvokeAsync(\"StockAlerts\")", layout);
        Assert.Contains("badge-counter", componentView);
        Assert.Contains("fa-bell", componentView);
    }

    [Fact]
    public void Movement_view_displays_critical_stock_alerts()
    {
        var view = ReadView("src/App/Views/Inventory/Movements.cshtml");

        Assert.Contains("CriticalStockAlerts", view);
        Assert.Contains("Estoque critico", view);
    }

    [Fact]
    public void Movement_form_does_not_expose_business_reference()
    {
        var view = ReadView("src/App/Views/Inventory/Movements.cshtml");

        Assert.DoesNotContain("asp-for=\"BusinessReference\"", view);
    }

    [Fact]
    public void Movement_form_conditionally_requires_reason_for_outbound()
    {
        var view = ReadView("src/App/Views/Inventory/Movements.cshtml");

        Assert.Contains("data-stock-movement-reason-group", view);
        Assert.Contains("reasonField.required = isOutbound", view);
        Assert.Contains("reasonField.disabled = !isOutbound", view);
    }

    [Fact]
    public void Movement_form_uses_stock_movement_type_options_including_adjustment()
    {
        var view = ReadView("src/App/Views/Inventory/Movements.cshtml");

        Assert.Contains("GetEnumSelectList<Craftsman.Domain.Inventory.Entities.StockMovementType>()", view);
        Assert.Contains(nameof(StockMovementType.Adjustment), Enum.GetNames<StockMovementType>());
    }

    [Fact]
    public void Movement_view_displays_standard_success_and_error_alerts()
    {
        var view = ReadView("src/App/Views/Inventory/Movements.cshtml");

        Assert.Contains("TempData[\"Success\"]", view);
        Assert.Contains("alert alert-success", view);
        Assert.Contains("asp-validation-summary=\"All\"", view);
        Assert.Contains("alert alert-danger", view);
    }

    private static string ReadView(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Craftsman.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new DirectoryNotFoundException("Repository root was not found.");
        }

        return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
    }
}
