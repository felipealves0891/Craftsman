using Craftsman.UI.Components.Finance;
using Craftsman.UI.Components.Inventory;
using Craftsman.UI.Components.ManualOrders;
using Craftsman.UI.Components.Orders;
using Craftsman.UI.Components.Products;
using Craftsman.UI.Components.Shared;
using Craftsman.UI.Components.Shipments;
using Craftsman.UI.Components.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Craftsman.Tests.Application;

public sealed class CatalogAndFinanceComponentTests
{
    [Fact]
    public async Task Finance_page_renders_empty_state_kpis_filters_and_messages()
    {
        var html = await RenderAsync<FinanceIndexPage>(new Dictionary<string, object?>
        {
            [nameof(FinanceIndexPage.Kpis)] = new[]
            {
                new FinanceIndexPage.KpiCard("Receita", "fa-dollar-sign", "R$ 100,00")
            },
            [nameof(FinanceIndexPage.From)] = "2031-06-01",
            [nameof(FinanceIndexPage.To)] = "2031-06-30",
            [nameof(FinanceIndexPage.SuccessMessage)] = "Apuração concluída",
            [nameof(FinanceIndexPage.ErrorMessage)] = "Aviso de exemplo"
        });

        Assert.Contains("Apura", html);
        Assert.Contains("Receita", html);
        Assert.Contains("R$ 100,00", html);
        Assert.Contains("value=\"2031-06-01\"", html);
        Assert.Contains("value=\"2031-06-30\"", html);
        Assert.Contains("conclu", html);
        Assert.Contains("Aviso de exemplo", html);
        Assert.Contains("Nenhuma apura", html);
        Assert.Contains("/Finance/Pending", html);
    }

    [Fact]
    public async Task Finance_page_renders_settlement_rows_and_details_link()
    {
        var orderId = Guid.NewGuid();
        var html = await RenderAsync<FinanceIndexPage>(new Dictionary<string, object?>
        {
            [nameof(FinanceIndexPage.Settlements)] = new[]
            {
                new FinanceIndexPage.SettlementRow(
                    orderId, "SO-42", "R$ 100,00", "R$ 5,00", "R$ 35,00", "R$ 60,00",
                    "Calculada", "calculated", "15/06/2031")
            }
        });

        Assert.Contains("SO-42", html);
        Assert.Contains("R$ 60,00", html);
        Assert.Contains("status-calculated", html);
        Assert.Contains($"/Finance/Details?orderId={orderId}", html);
        Assert.DoesNotContain("Nenhuma apuração encontrada", html);
    }

    [Fact]
    public async Task Inventory_pages_render_empty_states_and_material_balance_and_movement_rows()
    {
        var emptyBalances = await RenderAsync<InventoryBalancesPage>(new Dictionary<string, object?>());
        Assert.Contains("Nenhum saldo registrado", emptyBalances);

        var materialId = Guid.NewGuid();
        var balances = await RenderAsync<InventoryBalancesPage>(new Dictionary<string, object?>
        {
            [nameof(InventoryBalancesPage.Balances)] = new[]
            {
                new InventoryBalancesPage.BalanceRow(materialId, "Tecido", "12", "m", "<span>Normal</span>")
            }
        });
        Assert.Contains("Tecido", balances);
        Assert.Contains("12 m", balances);
        Assert.Contains("/Inventory/History/" + materialId, balances);

        var emptyHistory = await RenderAsync<InventoryHistoryPage>(new Dictionary<string, object?>());
        Assert.Contains("Nenhum movimento registrado", emptyHistory);

        var history = await RenderAsync<InventoryHistoryPage>(new Dictionary<string, object?>
        {
            [nameof(InventoryHistoryPage.Movements)] = new[]
            {
                new InventoryHistoryPage.MovementRow("Inbound", "+5", "R$ 2,00", "R$ 10,00", "Compra", "PO-1", "15/06/2031")
            }
        });
        Assert.Contains("Inbound", history);
        Assert.Contains("PO-1", history);
        Assert.Contains("R$ 10,00", history);
    }

    [Fact]
    public async Task Inventory_and_product_index_pages_render_empty_and_populated_rows()
    {
        var emptyMaterials = await RenderAsync<InventoryIndexPage>(new Dictionary<string, object?>());
        Assert.Contains("Nenhuma mat", emptyMaterials);

        var materialId = Guid.NewGuid();
        var materials = await RenderAsync<InventoryIndexPage>(new Dictionary<string, object?>
        {
            [nameof(InventoryIndexPage.Materials)] = new[]
            {
                new InventoryIndexPage.MaterialRow(materialId, "Algodão", "kg", "Active", "active", "10", "2", "<span>Baixo</span>")
            }
        });
        Assert.Contains("Algod", materials);
        Assert.Contains("/Inventory/EditMaterial/" + materialId, materials);

        var emptyProducts = await RenderAsync<ProductsIndexPage>(new Dictionary<string, object?>());
        Assert.Contains("Nenhum produto cadastrado", emptyProducts);

        var productId = Guid.NewGuid();
        var products = await RenderAsync<ProductsIndexPage>(new Dictionary<string, object?>
        {
            [nameof(ProductsIndexPage.Products)] = new[]
            {
                new ProductsIndexPage.ProductRow(productId, "Caneca", "Active", "active", 2, "R$ 20,00", 3)
            }
        });
        Assert.Contains("Caneca", products);
        Assert.Contains("2 hora(s)", products);
        Assert.Contains("3 item(ns)", products);
        Assert.Contains("/Products/BillOfMaterials/" + productId, products);
        Assert.Contains("/Products/Edit/" + productId, products);
    }

    [Fact]
    public async Task Orders_page_renders_import_summary_failures_and_order_details()
    {
        var orderId = Guid.NewGuid();
        var html = await RenderAsync<OrdersIndexPage>(new Dictionary<string, object?>
        {
            [nameof(OrdersIndexPage.RequestVerificationToken)] = "csrf-orders",
            [nameof(OrdersIndexPage.LastImport)] = new OrdersIndexPage.ImportSummary(2, 1, ["Pedido inválido"]),
            [nameof(OrdersIndexPage.Orders)] = new[]
            {
                new OrdersIndexPage.OrderRow(orderId, "Manual", "Ana", "R$ 5", "20/06/2031",
                    "Planejado", "planned", 2, "15/06/2031")
            }
        });

        Assert.Contains("Importados", html);
        Assert.Contains("Pedido inv", html);
        Assert.Contains("Ana", html);
        Assert.Contains("/Orders/Details/" + orderId, html);
        Assert.Contains("value=\"csrf-orders\"", html);
    }

    [Fact]
    public async Task Orders_page_shows_empty_wallet_and_import_summary_without_failures()
    {
        var html = await RenderAsync<OrdersIndexPage>(new Dictionary<string, object?>
        {
            [nameof(OrdersIndexPage.LastImport)] = new OrdersIndexPage.ImportSummary(3, 2, []),
            [nameof(OrdersIndexPage.Orders)] = Array.Empty<OrdersIndexPage.OrderRow>()
        });

        Assert.Contains("Nenhum pedido na carteira", html);
        Assert.Contains("Importados", html);
        Assert.Contains("Ignorados", html);
        Assert.DoesNotContain("Falhas na importacao", html);
    }

    [Fact]
    public async Task Shipments_page_renders_filters_errors_and_link_or_plain_tracking_codes()
    {
        var shipmentId = Guid.NewGuid();
        var html = await RenderAsync<ShipmentsIndexPage>(new Dictionary<string, object?>
        {
            [nameof(ShipmentsIndexPage.ErrorMessage)] = "Falha de atualização",
            [nameof(ShipmentsIndexPage.RequestVerificationToken)] = "csrf-shipments",
            [nameof(ShipmentsIndexPage.StatusOptions)] = new[] { new SelectOption("Delivered", "Entregue", true) },
            [nameof(ShipmentsIndexPage.Shipments)] = new[]
            {
                new ShipmentsIndexPage.ShipmentRow(shipmentId, Guid.NewGuid(), "https://track.test/AB-123", true,
                    "Em trânsito", "in-transit", "15/06/2031"),
                new ShipmentsIndexPage.ShipmentRow(Guid.NewGuid(), Guid.NewGuid(), "ZX-456", false,
                    "Postado", "posted", "16/06/2031"),
                new ShipmentsIndexPage.ShipmentRow(Guid.NewGuid(), Guid.NewGuid(), null, true,
                    "Sem rastreio", "pending", "17/06/2031")
            }
        });

        Assert.Contains("Falha de", html);
        Assert.Contains("value=\"Delivered\"", html);
        Assert.Contains("https://track.test/AB-123", html);
        Assert.Contains("AB-123", html);
        Assert.Contains("ZX-456", html);
        Assert.Contains("value=\"Cancelled\"", html);
        Assert.Contains("value=\"csrf-shipments\"", html);

        var empty = await RenderAsync<ShipmentsIndexPage>(new Dictionary<string, object?>());
        Assert.Contains("Nenhum envio encontrado", empty);
        Assert.DoesNotContain("role=\"alert\"", empty);
    }

    [Fact]
    public async Task Users_page_lists_email_and_roles()
    {
        var html = await RenderAsync<UsersIndexPage>(new Dictionary<string, object?>
        {
            [nameof(UsersIndexPage.Users)] = new[]
            {
                new UsersIndexPage.UserRow("ana@example.test", "Admin"),
                new UsersIndexPage.UserRow("joao@example.test", "Operador")
            }
        });

        Assert.Contains("ana@example.test", html);
        Assert.Contains("Admin", html);
        Assert.Contains("joao@example.test", html);
        Assert.Contains("Operador", html);
        Assert.Contains("/Users/Create", html);
    }

    [Fact]
    public async Task Order_details_renders_customer_discount_alerts_and_related_records()
    {
        var orderId = Guid.NewGuid();
        var html = await RenderAsync<OrderDetailsPage>(new Dictionary<string, object?>
        {
            [nameof(OrderDetailsPage.OrderId)] = orderId,
            [nameof(OrderDetailsPage.Source)] = "Manual",
            [nameof(OrderDetailsPage.ExternalOrderId)] = "MAN-42",
            [nameof(OrderDetailsPage.CustomerName)] = "Ana",
            [nameof(OrderDetailsPage.CustomerPhone)] = "11999990000",
            [nameof(OrderDetailsPage.CustomerEmail)] = "ana@example.test",
            [nameof(OrderDetailsPage.DiscountAmount)] = 5m,
            [nameof(OrderDetailsPage.DiscountDescription)] = "Cupom",
            [nameof(OrderDetailsPage.Status)] = "Normalizado",
            [nameof(OrderDetailsPage.ShippingDate)] = "20/06/2031",
            [nameof(OrderDetailsPage.SuccessMessage)] = "Salvo",
            [nameof(OrderDetailsPage.ErrorMessage)] = "Erro exibido",
            [nameof(OrderDetailsPage.StockAlertsMessage)] = "Estoque baixo",
            [nameof(OrderDetailsPage.CanChangeOrder)] = true,
            [nameof(OrderDetailsPage.DeleteProcessId)] = Guid.NewGuid(),
            [nameof(OrderDetailsPage.RequestVerificationToken)] = "csrf-order-details",
            [nameof(OrderDetailsPage.Items)] = new[]
            {
                new OrderDetailsPage.OrderItemRow("SKU-1", "Caneca", 2, "R$ 20,00")
            },
            [nameof(OrderDetailsPage.ProductionTasks)] = new[]
            {
                new OrderDetailsPage.ProductionTaskRow(Guid.NewGuid(), Guid.NewGuid(), 2, "Planejada")
            },
            [nameof(OrderDetailsPage.Shipments)] = new[]
            {
                new OrderDetailsPage.ShipmentRow("AB-123", "Postado", "15/06/2031")
            }
        });

        Assert.Contains("MAN-42", html);
        Assert.Contains("Ana", html);
        Assert.Contains("ana@example.test", html);
        Assert.Contains("Cupom", html);
        Assert.Contains("Salvo", html);
        Assert.Contains("Erro exibido", html);
        Assert.Contains("Estoque baixo", html);
        Assert.Contains("/ManualOrders/Edit/" + orderId, html);
        Assert.Contains("/Orders/Delete/" + orderId, html);
        Assert.DoesNotContain("/Orders/SendToProduction/" + orderId, html, StringComparison.Ordinal);
        Assert.Contains("SKU-1", html);
        Assert.Contains("AB-123", html);
        Assert.Contains("csrf-order-details", html);
    }

    [Fact]
    public async Task Order_details_shows_defaults_and_production_action_when_related_data_is_empty()
    {
        var orderId = Guid.NewGuid();
        var html = await RenderAsync<OrderDetailsPage>(new Dictionary<string, object?>
        {
            [nameof(OrderDetailsPage.OrderId)] = orderId,
            [nameof(OrderDetailsPage.Source)] = "Shopee",
            [nameof(OrderDetailsPage.ExternalOrderId)] = "SO-7",
            [nameof(OrderDetailsPage.CustomerName)] = " ",
            [nameof(OrderDetailsPage.DiscountAmount)] = 0m,
            [nameof(OrderDetailsPage.Status)] = "Pendente",
            [nameof(OrderDetailsPage.CanChangeOrder)] = false
        });

        Assert.Contains("Sem cliente", html);
        Assert.Contains("Sem desconto", html);
        Assert.DoesNotContain("SKU-", html);
        Assert.Contains("/Orders/SendToProduction/" + orderId, html);
        Assert.DoesNotContain("/ManualOrders/Edit/" + orderId, html);
        Assert.DoesNotContain("class=\"alert alert-success\"", html);
        Assert.DoesNotContain("class=\"alert alert-danger\"", html);
        Assert.DoesNotContain("class=\"alert alert-warning\"", html);
    }

    [Fact]
    public async Task Order_details_shows_revert_production_action_when_order_is_eligible()
    {
        var orderId = Guid.NewGuid();
        var html = await RenderAsync<OrderDetailsPage>(new Dictionary<string, object?>
        {
            [nameof(OrderDetailsPage.OrderId)] = orderId,
            [nameof(OrderDetailsPage.Source)] = "Manual",
            [nameof(OrderDetailsPage.ExternalOrderId)] = "MAN-REVERT-1",
            [nameof(OrderDetailsPage.Status)] = "Pronto para producao",
            [nameof(OrderDetailsPage.CanRevertProduction)] = true,
            [nameof(OrderDetailsPage.RequestVerificationToken)] = "csrf-revert",
            [nameof(OrderDetailsPage.ProductionTasks)] = new[]
            {
                new OrderDetailsPage.ProductionTaskRow(Guid.NewGuid(), Guid.NewGuid(), 1, "Planejada")
            }
        });

        Assert.Contains("/Orders/RevertProduction/" + orderId, html);
        Assert.Contains("csrf-revert", html);
        Assert.Contains("Reverter envio para producao", html);
    }

    [Fact]
    public async Task Settlement_details_renders_summary_and_empty_items_state()
    {
        var orderId = Guid.NewGuid();
        var html = await RenderAsync<FinancialSettlementDetailsPage>(new Dictionary<string, object?>
        {
            [nameof(FinancialSettlementDetailsPage.OrderId)] = orderId,
            [nameof(FinancialSettlementDetailsPage.RevenueAmount)] = 100m,
            [nameof(FinancialSettlementDetailsPage.ProductionCostAmount)] = 25m,
            [nameof(FinancialSettlementDetailsPage.ShippingCostAmount)] = 10m,
            [nameof(FinancialSettlementDetailsPage.DiscountAmount)] = 5m,
            [nameof(FinancialSettlementDetailsPage.TotalCostAmount)] = 40m,
            [nameof(FinancialSettlementDetailsPage.MarginAmount)] = 60m,
            [nameof(FinancialSettlementDetailsPage.Status)] = "Calculated",
            [nameof(FinancialSettlementDetailsPage.CalculatedAt)] = DateTimeOffset.Parse("2031-06-15T12:00:00+00:00")
        });

        Assert.Contains(orderId.ToString(), html);
        Assert.Contains("Calculated", html);
        Assert.Contains("Nenhum item", html);
        Assert.Contains("/Finance", html);
    }

    [Fact]
    public async Task Settlement_details_renders_items_when_order_items_exist()
    {
        var html = await RenderAsync<FinancialSettlementDetailsPage>(new Dictionary<string, object?>
        {
            [nameof(FinancialSettlementDetailsPage.Items)] = new[]
            {
                new FinancialSettlementDetailsPage.OrderItemRow("SKU-9", "Bolsa", 3, "R$ 20,00")
            }
        });

        Assert.Contains("SKU-9", html);
        Assert.Contains("Bolsa", html);
        Assert.DoesNotContain("Nenhum item", html);
    }

    [Fact]
    public async Task Shipment_create_page_renders_validation_and_empty_order_selection()
    {
        var html = await RenderAsync<ShipmentCreatePage>(new Dictionary<string, object?>
        {
            [nameof(ShipmentCreatePage.RequestVerificationToken)] = "csrf-shipment",
            [nameof(ShipmentCreatePage.TrackingCode)] = "AB-123",
            [nameof(ShipmentCreatePage.ValidationSummaryErrors)] = new[] { "Corrija os dados" },
            [nameof(ShipmentCreatePage.OrderIdErrors)] = new[] { "Pedido obrigatório" },
            [nameof(ShipmentCreatePage.TrackingCodeErrors)] = new[] { "Rastreio obrigatório" }
        });

        Assert.Contains("csrf-shipment", html);
        Assert.Contains("AB-123", html);
        Assert.Contains("Corrija os dados", html);
        Assert.Contains("Pedido obrig", html);
        Assert.Contains("Rastreio obrig", html);
        Assert.Contains("Nenhum pedido disponivel", html);
    }

    [Fact]
    public async Task Shipment_create_page_renders_available_order_and_items()
    {
        var orderId = Guid.NewGuid();
        var html = await RenderAsync<ShipmentCreatePage>(new Dictionary<string, object?>
        {
            [nameof(ShipmentCreatePage.OrderId)] = orderId,
            [nameof(ShipmentCreatePage.SelectedOrderText)] = "Shopee SO-1",
            [nameof(ShipmentCreatePage.AvailableOrders)] = new[]
            {
                new ShipmentCreatePage.OrderSelectionRow(orderId, "Shopee", "SO-1", "Pronto", "20/06/2031",
                    "Shopee SO-1", "Shopee SO-1 Caneca", [new ShipmentCreatePage.OrderSelectionItemRow("SKU-1", "Caneca", 1, "R$ 20,00")])
            }
        });

        Assert.Contains(orderId.ToString(), html);
        Assert.Contains("SO-1", html);
        Assert.Contains("Caneca", html);
        Assert.Contains("R$ 20,00", html);
        Assert.Contains("data-order-option", html);
        Assert.DoesNotContain("Nenhum pedido disponivel", html);
    }

    [Fact]
    public async Task Manual_order_create_page_renders_validation_and_blank_order_form()
    {
        var html = await RenderAsync<ManualOrderCreatePage>(new Dictionary<string, object?>
        {
            [nameof(ManualOrderCreatePage.RequestVerificationToken)] = "csrf-manual",
            [nameof(ManualOrderCreatePage.MinimumShippingDate)] = "2031-06-16",
            [nameof(ManualOrderCreatePage.ErrorMessage)] = "Erro no pedido",
            [nameof(ManualOrderCreatePage.ValidationSummaryErrors)] = new[] { "Informe uma origem" }
        });

        Assert.Contains("Novo pedido", html);
        Assert.Contains("csrf-manual", html);
        Assert.Contains("2031-06-16", html);
        Assert.Contains("Erro no pedido", html);
        Assert.Contains("Informe uma origem", html);
        Assert.Contains("/ManualOrders/CreateSource", html);
        Assert.Contains("/ManualOrders/Create\"", html);
        Assert.DoesNotContain("class=\"order-item-row manual-item-card\"", html);
    }

    [Fact]
    public async Task Manual_order_create_page_renders_existing_order_data_and_recalculation_notice()
    {
        var html = await RenderAsync<ManualOrderCreatePage>(new Dictionary<string, object?>
        {
            [nameof(ManualOrderCreatePage.FormAction)] = "/ManualOrders/EditOrder",
            [nameof(ManualOrderCreatePage.PageEyebrow)] = "Editar pedido",
            [nameof(ManualOrderCreatePage.PageTitle)] = "Editar MAN-2",
            [nameof(ManualOrderCreatePage.SubmitLabel)] = "Salvar alteracoes",
            [nameof(ManualOrderCreatePage.RequestVerificationToken)] = "csrf-edit",
            [nameof(ManualOrderCreatePage.OrderSources)] = new[] { new SelectOption("Manual", "Manual", true) },
            [nameof(ManualOrderCreatePage.Customers)] = new[] { new ManualOrderCreatePage.CustomerOption("Ana", "11999990000", "ana@example.test") },
            [nameof(ManualOrderCreatePage.Products)] = new[] { new SelectOption("product-1", "Caneca", true) },
            [nameof(ManualOrderCreatePage.Items)] = new[]
            {
                new ManualOrderCreatePage.ManualOrderItemRow("SKU-1", "Estampa azul", 2, "25,00", "product-1")
            },
            [nameof(ManualOrderCreatePage.Reference)] = "MAN-2",
            [nameof(ManualOrderCreatePage.ShippingDate)] = "2031-06-20",
            [nameof(ManualOrderCreatePage.CustomerName)] = "Ana",
            [nameof(ManualOrderCreatePage.CustomerPhone)] = "11999990000",
            [nameof(ManualOrderCreatePage.CustomerEmail)] = "ana@example.test",
            [nameof(ManualOrderCreatePage.DiscountDescription)] = "Cupom",
            [nameof(ManualOrderCreatePage.DiscountValue)] = "10,00",
            [nameof(ManualOrderCreatePage.DiscountIsPercent)] = true,
            [nameof(ManualOrderCreatePage.ShowSettlementRecalculationWarning)] = true,
            [nameof(ManualOrderCreatePage.SuccessMessage)] = "Pedido atualizado"
        });

        Assert.Contains("Editar MAN-2", html);
        Assert.Contains("/ManualOrders/EditOrder", html);
        Assert.Contains("value=\"Manual\"", html);
        Assert.Contains("Ana", html);
        Assert.Contains("ana@example.test", html);
        Assert.Contains("Estampa azul", html);
        Assert.Contains("Caneca", html);
        Assert.Contains("25,00", html);
        Assert.Contains("Cupom", html);
        Assert.Contains("checked", html);
        Assert.Contains("recalculado automaticamente", html);
        Assert.Contains("Pedido atualizado", html);
    }

    [Fact]
    public async Task Manual_order_link_items_page_renders_items_products_and_error()
    {
        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var html = await RenderAsync<ManualOrderLinkItemsPage>(new Dictionary<string, object?>
        {
            [nameof(ManualOrderLinkItemsPage.OrderId)] = orderId,
            [nameof(ManualOrderLinkItemsPage.Source)] = "Shopee",
            [nameof(ManualOrderLinkItemsPage.ExternalOrderId)] = "SO-3",
            [nameof(ManualOrderLinkItemsPage.ErrorMessage)] = "Produto obrigatório",
            [nameof(ManualOrderLinkItemsPage.RequestVerificationToken)] = "csrf-link",
            [nameof(ManualOrderLinkItemsPage.Items)] = new[]
            {
                new ManualOrderLinkItemsPage.LinkItemRow(itemId, "SKU-X", "Bolsa", 4, "")
            },
            [nameof(ManualOrderLinkItemsPage.Products)] = new[]
            {
                new SelectOption("product-2", "Bolsa interna", true)
            }
        });

        Assert.Contains("SO-3", html);
        Assert.Contains("/Orders/Details/" + orderId, html);
        Assert.Contains("Produto obrig", html);
        Assert.Contains("SKU-X", html);
        Assert.Contains("Bolsa interna", html);
        Assert.Contains(itemId.ToString(), html);
        Assert.Contains("csrf-link", html);
    }

    private static async Task<string> RenderAsync<TComponent>(IDictionary<string, object?> parameters)
        where TComponent : IComponent
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        await using var renderer = new HtmlRenderer(serviceProvider, loggerFactory);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(parameters));
            return root.ToHtmlString();
        });
    }
}
