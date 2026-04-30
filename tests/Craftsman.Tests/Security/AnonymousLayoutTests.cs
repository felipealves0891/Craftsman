namespace Craftsman.Tests.Security;

public sealed class AnonymousLayoutTests
{
    [Fact]
    public void Account_views_use_anonymous_layout_by_default()
    {
        var accountViewStart = File.ReadAllText(ProjectPath("src", "App", "Views", "Account", "_ViewStart.cshtml"));
        var loginView = File.ReadAllText(ProjectPath("src", "App", "Views", "Account", "Login.cshtml"));

        Assert.Contains("Layout = \"_AnonymousLayout\"", accountViewStart);
        Assert.DoesNotContain("Layout = \"_Layout\"", loginView);
    }

    [Fact]
    public void Anonymous_layout_does_not_render_authenticated_chrome()
    {
        var anonymousLayout = File.ReadAllText(ProjectPath("src", "App", "Views", "Shared", "_AnonymousLayout.cshtml"));

        Assert.Contains("anonymous-shell", anonymousLayout);
        Assert.DoesNotContain("accordionSidebar", anonymousLayout);
        Assert.DoesNotContain("userDropdown", anonymousLayout);
        Assert.DoesNotContain("sticky-footer", anonymousLayout);
        Assert.DoesNotContain("asp-controller=\"Account\" asp-action=\"Logout\"", anonymousLayout);
    }

    [Fact]
    public void Login_keeps_client_side_validation_scripts()
    {
        var loginView = File.ReadAllText(ProjectPath("src", "App", "Views", "Account", "Login.cshtml"));

        Assert.Contains("_ValidationScriptsPartial", loginView);
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
