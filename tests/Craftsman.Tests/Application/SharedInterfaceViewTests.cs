namespace Craftsman.Tests.Application;

public sealed class SharedInterfaceViewTests
{
    [Theory]
    [InlineData("src/App/Views/Home/Index.cshtml", "typeof(HomeDashboard)")]
    [InlineData("src/App/Views/Home/Privacy.cshtml", "typeof(PrivacyPage)")]
    [InlineData("src/App/Views/Account/AccessDenied.cshtml", "typeof(AccessDeniedPage)")]
    [InlineData("src/App/Views/Finance/Details.cshtml", "typeof(FinancialSettlementDetailsPage)")]
    [InlineData("src/App/Views/Finance/Index.cshtml", "typeof(FinanceIndexPage)")]
    [InlineData("src/App/Views/Inventory/Balances.cshtml", "typeof(InventoryBalancesPage)")]
    [InlineData("src/App/Views/Inventory/History.cshtml", "typeof(InventoryHistoryPage)")]
    [InlineData("src/App/Views/Inventory/Index.cshtml", "typeof(InventoryIndexPage)")]
    [InlineData("src/App/Views/ManualOrders/Create.cshtml", "typeof(ManualOrderCreatePage)")]
    [InlineData("src/App/Views/ManualOrders/LinkItems.cshtml", "typeof(ManualOrderLinkItemsPage)")]
    [InlineData("src/App/Views/Orders/Details.cshtml", "typeof(OrderDetailsPage)")]
    [InlineData("src/App/Views/Orders/Index.cshtml", "typeof(OrdersIndexPage)")]
    [InlineData("src/App/Views/Production/Index.cshtml", "typeof(ProductionIndexPage)")]
    [InlineData("src/App/Views/Products/Index.cshtml", "typeof(ProductsIndexPage)")]
    [InlineData("src/App/Views/Shipments/Create.cshtml", "typeof(ShipmentCreatePage)")]
    [InlineData("src/App/Views/Shipments/Index.cshtml", "typeof(ShipmentsIndexPage)")]
    [InlineData("src/App/Views/Users/Index.cshtml", "typeof(UsersIndexPage)")]
    [InlineData("src/App/Views/Docs/Index.cshtml", "typeof(DocumentationPage)")]
    public void Migrated_views_render_shared_components(string relativePath, string componentType)
    {
        var view = File.ReadAllText(ProjectPath(relativePath.Split('/')));

        Assert.Contains(componentType, view);
        Assert.Contains("render-mode=\"Static\"", view);
    }

    [Fact]
    public void Home_dashboard_keeps_operational_shortcuts()
    {
        var component = File.ReadAllText(ProjectPath("src", "UI", "Components", "Home", "HomeDashboard.razor"));

        Assert.Contains("Painel Craftsman", component);
        Assert.Contains("href=\"/ManualOrders/Create\"", component);
        Assert.Contains("href=\"/Orders\"", component);
        Assert.Contains("href=\"/Inventory/Balances\"", component);
        Assert.Contains("href=\"/Production\"", component);
        Assert.Contains("href=\"/Finance\"", component);
        Assert.Contains("href=\"/Import\"", component);
        Assert.Contains("href=\"/Products\"", component);
        Assert.Contains("href=\"/Shipments\"", component);
    }

    [Fact]
    public void Financial_details_view_passes_model_values_to_shared_component()
    {
        var view = File.ReadAllText(ProjectPath("src", "App", "Views", "Finance", "Details.cshtml"));

        Assert.Contains("param-OrderId=\"@Model.OrderId\"", view);
        Assert.Contains("param-RevenueAmount=\"@Model.RevenueAmount\"", view);
        Assert.Contains("param-CalculatedAt=\"@Model.CalculatedAt\"", view);
    }

    [Fact]
    public void List_views_map_app_view_models_to_shared_component_rows()
    {
        var productsView = File.ReadAllText(ProjectPath("src", "App", "Views", "Products", "Index.cshtml"));
        var inventoryView = File.ReadAllText(ProjectPath("src", "App", "Views", "Inventory", "Index.cshtml"));
        var financeView = File.ReadAllText(ProjectPath("src", "App", "Views", "Finance", "Index.cshtml"));
        var ordersView = File.ReadAllText(ProjectPath("src", "App", "Views", "Orders", "Index.cshtml"));
        var productionView = File.ReadAllText(ProjectPath("src", "App", "Views", "Production", "Index.cshtml"));
        var shipmentsView = File.ReadAllText(ProjectPath("src", "App", "Views", "Shipments", "Index.cshtml"));

        Assert.Contains("new ProductsIndexPage.ProductRow", productsView);
        Assert.Contains("new InventoryIndexPage.MaterialRow", inventoryView);
        Assert.Contains("new FinanceIndexPage.SettlementRow", financeView);
        Assert.Contains("new OrdersIndexPage.OrderRow", ordersView);
        Assert.Contains("new ProductionIndexPage.CalendarDay", productionView);
        Assert.Contains("new ShipmentsIndexPage.ShipmentRow", shipmentsView);
    }

    [Fact]
    public void Post_views_pass_antiforgery_token_to_shared_components()
    {
        var ordersView = File.ReadAllText(ProjectPath("src", "App", "Views", "Orders", "Index.cshtml"));
        var manualOrderView = File.ReadAllText(ProjectPath("src", "App", "Views", "ManualOrders", "Create.cshtml"));
        var shipmentView = File.ReadAllText(ProjectPath("src", "App", "Views", "Shipments", "Create.cshtml"));

        Assert.Contains("GetAndStoreTokens(Context)", ordersView);
        Assert.Contains("param-RequestVerificationToken", manualOrderView);
        Assert.Contains("param-RequestVerificationToken", shipmentView);
    }

    [Fact]
    public void Shipment_create_view_passes_model_state_errors_to_static_component()
    {
        var view = File.ReadAllText(ProjectPath("src", "App", "Views", "Shipments", "Create.cshtml"));
        var component = File.ReadAllText(ProjectPath("src", "UI", "Components", "Shipments", "ShipmentCreatePage.razor"));

        Assert.Contains("ViewData.ModelState", view);
        Assert.Contains("param-ValidationSummaryErrors", view);
        Assert.Contains("param-OrderIdErrors", view);
        Assert.Contains("param-TrackingCodeErrors", view);
        Assert.Contains("role=\"alert\"", component);
        Assert.Contains("OrderIdErrors", component);
        Assert.Contains("TrackingCodeErrors", component);
        Assert.Contains("field-validation-error", component);
    }

    [Fact]
    public void Manual_order_create_view_passes_model_state_errors_to_static_component()
    {
        var view = File.ReadAllText(ProjectPath("src", "App", "Views", "ManualOrders", "Create.cshtml"));
        var component = File.ReadAllText(ProjectPath("src", "UI", "Components", "ManualOrders", "ManualOrderCreatePage.razor"));
        var controller = File.ReadAllText(ProjectPath("src", "App", "Controllers", "ManualOrdersController.cs"));

        Assert.Contains("ViewData.ModelState", view);
        Assert.Contains("param-ValidationSummaryErrors", view);
        Assert.Contains("ValidationSummaryErrors", component);
        Assert.Contains("role=\"alert\"", component);
        Assert.DoesNotContain("Erro inesperado, tente novamente mais tarde!", controller);
    }

    [Fact]
    public void Product_mappings_view_displays_required_field_validation_messages()
    {
        var view = File.ReadAllText(ProjectPath("src", "App", "Views", "Products", "Mappings.cshtml"));

        Assert.Contains("asp-validation-summary=\"ModelOnly\"", view);
        Assert.Contains("asp-validation-for=\"Source\"", view);
        Assert.Contains("asp-validation-for=\"ExternalItemId\"", view);
        Assert.Contains("asp-validation-for=\"ProductId\"", view);
    }

    [Fact]
    public void Operational_post_pages_display_backend_errors_from_temp_data()
    {
        var productionView = File.ReadAllText(ProjectPath("src", "App", "Views", "Production", "Index.cshtml"));
        var productionComponent = File.ReadAllText(ProjectPath("src", "UI", "Components", "Production", "ProductionIndexPage.razor"));
        var productionController = File.ReadAllText(ProjectPath("src", "App", "Controllers", "ProductionController.cs"));
        var shipmentsView = File.ReadAllText(ProjectPath("src", "App", "Views", "Shipments", "Index.cshtml"));
        var shipmentsComponent = File.ReadAllText(ProjectPath("src", "UI", "Components", "Shipments", "ShipmentsIndexPage.razor"));
        var shipmentsController = File.ReadAllText(ProjectPath("src", "App", "Controllers", "ShipmentsController.cs"));

        Assert.Contains("TempData[\"Error\"]", productionController);
        Assert.Contains("TempData[\"Error\"]", shipmentsController);
        Assert.Contains("TempData[\"Error\"]", productionView);
        Assert.Contains("TempData[\"Error\"]", shipmentsView);
        Assert.Contains("param-ErrorMessage", productionView);
        Assert.Contains("param-ErrorMessage", shipmentsView);
        Assert.Contains("role=\"alert\"", productionComponent);
        Assert.Contains("role=\"alert\"", shipmentsComponent);
    }

    [Fact]
    public void Manual_order_item_form_uses_observation_textarea_and_full_width_product_select()
    {
        var component = File.ReadAllText(ProjectPath("src", "UI", "Components", "ManualOrders", "ManualOrderCreatePage.razor"));
        var styles = File.ReadAllText(ProjectPath("src", "App", "wwwroot", "css", "site.css"));

        Assert.DoesNotContain("Items[@i].ExternalItemId", component);
        Assert.DoesNotContain("product-filter", component);
        Assert.Contains("Observacao", component);
        Assert.Contains("<textarea id=\"Items_@(i)__Description\" name=\"Items[@i].Description\"", component);
        Assert.Contains(".manual-item-product", styles);
        Assert.Contains("grid-column: span 3;", styles);
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
