namespace Craftsman.Tests.Shipping;

public sealed class ShipmentCreateViewTests
{
    [Fact]
    public void Create_view_uses_order_selection_modal()
    {
        var view = ReadView("src/App/Views/Shipments/Create.cshtml");

        Assert.Contains("data-order-modal-trigger", view);
        Assert.Contains("data-shipment-order-modal", view);
        Assert.Contains("data-order-select", view);
    }

    [Fact]
    public void Create_view_keeps_order_id_hidden_from_manual_input()
    {
        var view = ReadView("src/App/Views/Shipments/Create.cshtml");

        Assert.Contains("asp-for=\"OrderId\" type=\"hidden\"", view);
        Assert.DoesNotContain("asp-for=\"OrderId\" class=\"form-control\"", view);
    }

    [Fact]
    public void Create_view_displays_order_items_in_modal()
    {
        var view = ReadView("src/App/Views/Shipments/Create.cshtml");

        Assert.Contains("data-order-items", view);
        Assert.Contains("Descricao", view);
        Assert.Contains("Quantidade", view);
        Assert.Contains("Valor unitario", view);
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
