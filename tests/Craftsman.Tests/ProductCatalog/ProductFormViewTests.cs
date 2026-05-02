namespace Craftsman.Tests.ProductCatalog;

public sealed class ProductFormViewTests
{
    [Fact]
    public void Product_edit_view_has_production_hours_and_hourly_rate_fields()
    {
        var view = File.ReadAllText(ProjectPath("src", "App", "Views", "Products", "Edit.cshtml"));

        Assert.Contains("asp-for=\"ProductionDurationHours\"", view);
        Assert.Contains("asp-for=\"HourlyRate\"", view);
    }

    [Fact]
    public void Product_views_do_not_expose_barcode_or_scanner()
    {
        var editView = File.ReadAllText(ProjectPath("src", "App", "Views", "Products", "Edit.cshtml"));
        var indexView = File.ReadAllText(ProjectPath("src", "App", "Views", "Products", "Index.cshtml"));

        Assert.DoesNotContain("Barcode", editView);
        Assert.DoesNotContain("barcode", editView, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("product-barcode-scanner", editView);
        Assert.DoesNotContain("Barcode", indexView);
        Assert.DoesNotContain("barcode", indexView, StringComparison.OrdinalIgnoreCase);
    }

    private static string ProjectPath(params string[] segments)
    {
        var root = AppContext.BaseDirectory;
        for (var i = 0; i < 6; i++)
        {
            var candidate = Path.Combine(new[] { root }.Concat(segments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            root = Path.GetFullPath(Path.Combine(root, ".."));
        }

        throw new FileNotFoundException($"Could not find {string.Join(Path.DirectorySeparatorChar, segments)}.");
    }
}
