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
        var view = File.ReadAllText(ProjectPath("src", "App", "Views", "Docs", "Index.cshtml"));

        Assert.Contains("Comece por aqui", view);
        Assert.Contains("Checklist inicial de operacao", view);
        Assert.Contains("Fluxo principal", view);
        Assert.Contains("asp-controller=\"Import\"", view);
        Assert.Contains("asp-controller=\"ManualOrders\"", view);
        Assert.Contains("asp-controller=\"Products\"", view);
        Assert.Contains("asp-action=\"Mappings\"", view);
        Assert.Contains("asp-controller=\"Inventory\"", view);
        Assert.Contains("asp-controller=\"Production\"", view);
        Assert.Contains("asp-controller=\"Shipments\"", view);
        Assert.Contains("asp-controller=\"Finance\"", view);
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
