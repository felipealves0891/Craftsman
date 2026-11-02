using System.Security.Claims;
using Craftsman.App.Security;
using Microsoft.AspNetCore.Http;

namespace Craftsman.Tests.Security;

public sealed class HttpCurrentUserContextTests
{
    [Fact]
    public void Current_returns_anonymous_when_http_context_is_missing()
    {
        var context = new HttpCurrentUserContext(new HttpContextAccessor());

        var current = context.Current;

        Assert.Null(current.UserId);
        Assert.Null(current.UserName);
        Assert.Empty(current.RoleNames);
        Assert.Null(current.CorrelationId);
        Assert.Null(current.RequestPath);
    }

    [Fact]
    public void Current_reads_distinct_roles_and_request_identity()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-1";
        httpContext.Request.Path = "/orders";
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim(ClaimTypes.Name, "tester@example.com"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "Admin")
        ], "Test"));
        var context = new HttpCurrentUserContext(new HttpContextAccessor { HttpContext = httpContext });

        var current = context.Current;

        Assert.Equal(42, current.UserId);
        Assert.Equal("tester@example.com", current.UserName);
        Assert.Equal(["Admin"], current.RoleNames);
        Assert.Equal("trace-1", current.CorrelationId);
        Assert.Equal("/orders", current.RequestPath);
    }
}
