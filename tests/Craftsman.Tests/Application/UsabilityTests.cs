namespace Craftsman.Tests.Application;

public sealed class UsabilityTests
{
    [Fact]
    public void Decimal_editable_fields_use_text_inputs_with_decimal_keyboard()
    {
        var movementsView = File.ReadAllText(ProjectPath("src", "App", "Views", "Inventory", "Movements.cshtml"));
        var billOfMaterialsView = File.ReadAllText(ProjectPath("src", "App", "Views", "Products", "BillOfMaterials.cshtml"));

        Assert.Contains("asp-for=\"Quantity\" type=\"text\" inputmode=\"decimal\"", movementsView);
        Assert.Contains("asp-for=\"Items[i].QuantityPerUnit\" type=\"text\" inputmode=\"decimal\"", billOfMaterialsView);
        Assert.Contains("data-decimal-input", movementsView);
        Assert.Contains("data-decimal-input", billOfMaterialsView);
    }

    [Fact]
    public void Currency_editable_fields_use_brl_mask_marker_and_not_number_inputs()
    {
        var movementsView = File.ReadAllText(ProjectPath("src", "App", "Views", "Inventory", "Movements.cshtml"));
        var manualOrderView = File.ReadAllText(ProjectPath("src", "App", "Views", "ManualOrders", "Create.cshtml"));

        Assert.Contains("asp-for=\"UnitCostAmount\" type=\"text\" inputmode=\"decimal\"", movementsView);
        Assert.Contains("asp-for=\"Items[i].UnitPriceAmount\" type=\"text\" inputmode=\"decimal\"", manualOrderView);
        Assert.Contains("data-currency-input", movementsView);
        Assert.Contains("data-currency-input", manualOrderView);
        Assert.DoesNotContain("asp-for=\"UnitCostAmount\" type=\"number\"", movementsView);
        Assert.DoesNotContain("asp-for=\"Items[i].UnitPriceAmount\" type=\"number\"", manualOrderView);
    }

    [Fact]
    public void Site_script_formats_brl_normalizes_submit_and_closes_mobile_sidebar()
    {
        var script = File.ReadAllText(ProjectPath("src", "App", "wwwroot", "js", "site.js"));

        Assert.Contains("Intl.NumberFormat('pt-BR'", script);
        Assert.Contains("style: 'currency'", script);
        Assert.Contains("currency: 'BRL'", script);
        Assert.Contains("normalizeNumericInputs(event.target)", script);
        Assert.Contains("data-currency-input", script);
        Assert.Contains("data-decimal-input", script);
        Assert.Contains("(max-width: 767.98px)", script);
        Assert.Contains("closeMobileSidebarSubmenus", script);
        Assert.Contains("classList.remove('show')", script);
        Assert.Contains("validator.methods.number", script);
    }

    [Fact]
    public void Application_configures_pt_br_request_culture()
    {
        var program = File.ReadAllText(ProjectPath("src", "App", "Program.cs"));

        Assert.Contains("new CultureInfo(\"pt-BR\")", program);
        Assert.Contains("UseRequestLocalization", program);
        Assert.Contains("DefaultRequestCulture = new RequestCulture(supportedCulture)", program);
    }

    [Fact]
    public void Layout_loads_global_site_script()
    {
        var layout = File.ReadAllText(ProjectPath("src", "App", "Views", "Shared", "_Layout.cshtml"));

        Assert.Contains("~/js/site.js", layout);
    }

    private static string ProjectPath(params string[] paths)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Craftsman.slnx")))
        {
            current = current.Parent;
        }

        Assert.NotNull(current);
        return Path.Combine([current!.FullName, .. paths]);
    }
}
