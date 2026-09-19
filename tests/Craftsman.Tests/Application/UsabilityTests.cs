namespace Craftsman.Tests.Application;

public sealed class UsabilityTests
{
    [Fact]
    public void Quantity_editable_fields_use_text_inputs_with_numeric_keyboard()
    {
        var movementsView = File.ReadAllText(ProjectPath("src", "App", "Views", "Inventory", "Movements.cshtml"));
        var billOfMaterialsView = File.ReadAllText(ProjectPath("src", "App", "Views", "Products", "BillOfMaterials.cshtml"));

        Assert.Contains("asp-for=\"Quantity\" type=\"text\" inputmode=\"numeric\"", movementsView);
        Assert.Contains("data-numeric-input", movementsView);
        Assert.Contains("asp-for=\"Items[i].QuantityPerUnit\" type=\"text\" inputmode=\"numeric\"", billOfMaterialsView);
        Assert.Contains("pattern=\"[0-9]*\"", billOfMaterialsView);
        Assert.Contains("step=\"1\"", billOfMaterialsView);
        Assert.DoesNotContain("asp-for=\"Items[i].QuantityPerUnit\" type=\"text\" inputmode=\"decimal\"", billOfMaterialsView);
    }

    [Fact]
    public void Bill_of_materials_quantity_uses_integer_input_and_dynamic_add_button()
    {
        var billOfMaterialsView = File.ReadAllText(ProjectPath("src", "App", "Views", "Products", "BillOfMaterials.cshtml"));

        Assert.Contains("data-add-bill-of-materials-row", billOfMaterialsView);
        Assert.Contains("fa-plus", billOfMaterialsView);
        Assert.Contains("Items[${index}]", billOfMaterialsView);
    }

    [Fact]
    public void Bill_of_materials_quantity_input_model_uses_integer_quantity()
    {
        Assert.Equal(typeof(int), typeof(Craftsman.App.Models.BillOfMaterialsItemInputModel)
            .GetProperty(nameof(Craftsman.App.Models.BillOfMaterialsItemInputModel.QuantityPerUnit))
            ?.PropertyType);
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
