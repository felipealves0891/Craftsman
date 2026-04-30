namespace Craftsman.Tests.ProductCatalog;

public sealed class ProductBarcodeScannerViewTests
{
    [Fact]
    public void Product_edit_view_has_barcode_field_camera_button_and_scanner_scripts()
    {
        var view = File.ReadAllText(ProjectPath("src", "App", "Views", "Products", "Edit.cshtml"));

        Assert.Contains("asp-for=\"Barcode\"", view);
        Assert.Contains("data-barcode-start", view);
        Assert.Contains("data-barcode-reader", view);
        Assert.Contains("html5-qrcode@2.3.8/html5-qrcode.min.js", view);
        Assert.Contains("~/js/product-barcode-scanner.js", view);
    }

    [Fact]
    public void Product_barcode_scanner_script_prefers_mobile_back_camera_and_preserves_manual_fallback()
    {
        var script = File.ReadAllText(ProjectPath("src", "App", "wwwroot", "js", "product-barcode-scanner.js"));

        Assert.Contains("facingMode: \"environment\"", script);
        Assert.Contains("html5QrCode.start", script);
        Assert.Contains("Informe o codigo manualmente", script);
        Assert.Contains("input.value = decodedText", script);
        Assert.Contains("html5QrCode.stop()", script);
    }

    [Fact]
    public void Product_barcode_scanner_script_reports_detailed_camera_errors()
    {
        var script = File.ReadAllText(ProjectPath("src", "App", "wwwroot", "js", "product-barcode-scanner.js"));

        Assert.Contains("Permissao da camera negada", script);
        Assert.Contains("Camera nao encontrada", script);
        Assert.Contains("Camera ocupada ou indisponivel", script);
        Assert.Contains("biblioteca html5-qrcode nao carregada", script);
        Assert.Contains("mediaDevices/getUserMedia indisponivel", script);
        Assert.Contains("contexto inseguro", script);
        Assert.Contains("Detalhe: \" + name", script);
        Assert.Contains("setStatus(message, \"error\")", script);
        Assert.Contains("pagehide", script);
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
