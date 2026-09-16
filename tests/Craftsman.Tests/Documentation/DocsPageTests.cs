using Craftsman.App.Controllers;
using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Craftsman.Tests.Documentation;

public sealed class DocsPageTests
{
    [Fact]
    public void Docs_controller_uses_read_policy()
    {
        var policy = typeof(DocsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single()
            .Policy;

        Assert.Equal(ApplicationPolicies.Read, policy);
    }

    [Fact]
    public void Docs_index_returns_default_view()
    {
        var result = new DocsController().Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.ViewName);
    }

    [Fact]
    public void Authenticated_sidebar_links_to_documentation_for_read_roles()
    {
        var layout = File.ReadAllText(ProjectPath("src", "App", "Views", "Shared", "_Layout.cshtml"));

        Assert.Contains("var canRead =", layout);
        Assert.Contains("asp-controller=\"Docs\"", layout);
        Assert.Contains("Documenta&ccedil;&atilde;o", layout);
    }

    [Fact]
    public void Docs_page_explains_end_to_end_flow_and_required_links()
    {
        var view = File.ReadAllText(ProjectPath("src", "UI", "Components", "Docs", "DocumentationPage.razor"));

        Assert.Contains("Comece por aqui", view);
        Assert.Contains("Checklist inicial de operacao", view);
        Assert.Contains("Fluxo principal", view);
        Assert.Contains("href=\"/Import\"", view);
        Assert.Contains("href=\"/ManualOrders/Create\"", view);
        Assert.Contains("href=\"/Products\"", view);
        Assert.Contains("href=\"/Products/Mappings\"", view);
        Assert.Contains("href=\"/Inventory\"", view);
        Assert.Contains("href=\"/Production\"", view);
        Assert.Contains("href=\"/Shipments\"", view);
        Assert.Contains("href=\"/Finance\"", view);
    }

    [Fact]
    public void Docs_view_imports_shared_ui_component()
    {
        var view = File.ReadAllText(ProjectPath("src", "App", "Views", "Docs", "Index.cshtml"));

        Assert.Contains("ViewData[\"Title\"] = \"Documentacao\"", view);
        Assert.Contains("typeof(DocumentationPage)", view);
        Assert.DoesNotContain("Comece por aqui", view);
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
