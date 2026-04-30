using System.Security.Claims;
using Craftsman.Infra.Security;

namespace Craftsman.App.Security;

public sealed class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public CurrentUserInfo Current
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;
            var user = httpContext?.User;
            var userIdValue = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            _ = int.TryParse(userIdValue, out var userId);

            return new CurrentUserInfo(
                string.IsNullOrWhiteSpace(userIdValue) ? null : userId,
                user?.Identity?.IsAuthenticated == true ? user.Identity.Name : null,
                user?.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct().ToArray() ?? Array.Empty<string>(),
                httpContext?.TraceIdentifier,
                httpContext?.Request.Path.Value);
        }
    }
}
