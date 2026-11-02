using System.Security.Claims;
using Craftsman.App.Controllers;
using Craftsman.App.Models;
using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Craftsman.Tests.Security;

public sealed class AccountControllerTests
{
    [Fact]
    public void Login_get_returns_model_with_return_url()
    {
        var controller = CreateController(new RecordingAuditService(), new RecordingSignInManager());

        var result = Assert.IsType<ViewResult>(controller.Login("/orders"));
        var model = Assert.IsType<LoginInputModel>(result.Model);
        Assert.Equal("/orders", model.ReturnUrl);
    }

    [Fact]
    public async Task Login_post_returns_input_when_model_state_is_invalid()
    {
        var audit = new RecordingAuditService();
        var signIn = new RecordingSignInManager();
        var controller = CreateController(audit, signIn);
        controller.ModelState.AddModelError(nameof(LoginInputModel.Email), "required");
        var input = new LoginInputModel { Email = "ana@example.test", Password = "secret" };

        var result = Assert.IsType<ViewResult>(await controller.Login(input, CancellationToken.None));

        Assert.Same(input, result.Model);
        Assert.Equal(0, signIn.PasswordSignInCallCount);
        Assert.Empty(audit.Entries);
    }

    [Fact]
    public async Task Login_post_records_success_and_redirects_to_local_return_url()
    {
        var audit = new RecordingAuditService();
        var signIn = new RecordingSignInManager { Result = IdentitySignInResult.Success };
        var controller = CreateController(audit, signIn);

        var result = Assert.IsType<LocalRedirectResult>(await controller.Login(
            new LoginInputModel { Email = "ana@example.test", Password = "secret", RememberMe = true, ReturnUrl = "/orders" },
            CancellationToken.None));

        Assert.Equal("/orders", result.Url);
        Assert.Equal(1, signIn.PasswordSignInCallCount);
        Assert.Equal("ana@example.test", signIn.LastUserName);
        Assert.True(signIn.LastIsPersistent);
        Assert.Equal(AuditAction.LoginSucceeded, Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task Login_success_uses_home_redirect_for_non_local_return_url()
    {
        var signIn = new RecordingSignInManager { Result = IdentitySignInResult.Success };
        var controller = CreateController(new RecordingAuditService(), signIn);

        var result = Assert.IsType<LocalRedirectResult>(await controller.Login(
            new LoginInputModel { Email = "ana@example.test", Password = "secret", ReturnUrl = "https://example.test/steal" },
            CancellationToken.None));

        Assert.NotEqual("https://example.test/steal", result.Url);
        Assert.Contains("Home", result.Url);
    }

    [Fact]
    public async Task Login_post_records_failed_attempt_and_returns_validation_error()
    {
        var audit = new RecordingAuditService();
        var signIn = new RecordingSignInManager { Result = IdentitySignInResult.Failed };
        var controller = CreateController(audit, signIn);
        var input = new LoginInputModel { Email = "ana@example.test", Password = "wrong" };

        var result = Assert.IsType<ViewResult>(await controller.Login(input, CancellationToken.None));

        Assert.Same(input, result.Model);
        Assert.Contains(controller.ModelState[string.Empty]!.Errors, error => error.ErrorMessage == "Login invalido.");
        Assert.Equal(AuditAction.LoginFailed, Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task Logout_and_access_denied_record_audit_entries()
    {
        var audit = new RecordingAuditService();
        var signIn = new RecordingSignInManager();
        var controller = CreateController(audit, signIn);
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "ana@example.test")], "test"));
        controller.HttpContext.Request.Path = "/secure";

        var logout = Assert.IsType<RedirectToActionResult>(await controller.Logout(CancellationToken.None));
        Assert.Equal(nameof(AccountController.Login), logout.ActionName);
        Assert.Equal("ana@example.test", audit.Entries[0].EntityId);

        var denied = Assert.IsType<ViewResult>(await controller.AccessDenied(CancellationToken.None));
        Assert.Equal(2, audit.Entries.Count);
        Assert.Equal(AuditAction.AccessDenied, audit.Entries[1].Action);
        Assert.Equal("/secure", audit.Entries[1].After);
        Assert.Equal(1, signIn.SignOutCallCount);
    }

    [Fact]
    public async Task Access_denied_records_event_when_request_has_no_authenticated_identity()
    {
        var audit = new RecordingAuditService();
        var controller = CreateController(audit, new RecordingSignInManager());
        controller.HttpContext.User = new ClaimsPrincipal();
        controller.HttpContext.Request.Path = "/private";

        Assert.IsType<ViewResult>(await controller.AccessDenied(CancellationToken.None));

        var entry = Assert.Single(audit.Entries);
        Assert.Equal(AuditAction.AccessDenied, entry.Action);
        Assert.Null(entry.EntityId);
        Assert.Equal("/private", entry.After);

        var logout = Assert.IsType<RedirectToActionResult>(await controller.Logout(CancellationToken.None));
        Assert.Equal(nameof(AccountController.Login), logout.ActionName);
        Assert.Null(audit.Entries[1].EntityId);
    }

    private static AccountController CreateController(RecordingAuditService audit, RecordingSignInManager signIn)
    {
        var controller = new AccountController(signIn, audit);
        var httpContext = new DefaultHttpContext();
        var routeData = new RouteData();
        routeData.Routers.Add(new TestRouter());
        controller.ControllerContext = new ControllerContext(
            new ActionContext(httpContext, routeData, new ControllerActionDescriptor()));
        controller.Url = new UrlHelper(controller.ControllerContext);
        return controller;
    }

    private sealed class RecordingSignInManager : SignInManager<ApplicationUser>
    {
        public IdentitySignInResult Result { get; set; } = IdentitySignInResult.Failed;

        public int PasswordSignInCallCount { get; private set; }

        public int SignOutCallCount { get; private set; }

        public string? LastUserName { get; private set; }

        public bool LastIsPersistent { get; private set; }

        public RecordingSignInManager()
            : base(
                CreateUserManager(),
                new HttpContextAccessor(),
                new EmptyClaimsPrincipalFactory(),
                Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
                NullLogger<SignInManager<ApplicationUser>>.Instance,
                new AuthenticationSchemeProvider(Microsoft.Extensions.Options.Options.Create(new AuthenticationOptions())),
                new AlwaysConfirmedUser())
        {
        }

        public override Task<IdentitySignInResult> PasswordSignInAsync(
            string userName,
            string password,
            bool isPersistent,
            bool lockoutOnFailure)
        {
            PasswordSignInCallCount++;
            LastUserName = userName;
            LastIsPersistent = isPersistent;
            return Task.FromResult(Result);
        }

        public override Task SignOutAsync()
        {
            SignOutCallCount++;
            return Task.CompletedTask;
        }

        private static UserManager<ApplicationUser> CreateUserManager()
            => new(
                new EmptyUserStore(),
                Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
                new PasswordHasher<ApplicationUser>(),
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new ServiceCollection().BuildServiceProvider(),
                NullLogger<UserManager<ApplicationUser>>.Instance);
    }

    private sealed class EmptyUserStore : IUserStore<ApplicationUser>
    {
        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id.ToString());
        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);
        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) { user.UserName = userName; return Task.CompletedTask; }
        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);
        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) { user.NormalizedUserName = normalizedName; return Task.CompletedTask; }
        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(null);
        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(null);
        public void Dispose() { }
    }

    private sealed class EmptyClaimsPrincipalFactory : IUserClaimsPrincipalFactory<ApplicationUser>
    {
        public Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
            => Task.FromResult(new ClaimsPrincipal(new ClaimsIdentity("test")));
    }

    private sealed class TestRouter : IRouter
    {
        public VirtualPathData? GetVirtualPath(VirtualPathContext context)
        {
            var controller = context.Values.TryGetValue("controller", out var controllerValue) ? controllerValue : "";
            var action = context.Values.TryGetValue("action", out var actionValue) ? actionValue : "";
            return new VirtualPathData(this, $"/{controller}/{action}");
        }

        public Task RouteAsync(RouteContext context) => Task.CompletedTask;
    }

    private sealed class AlwaysConfirmedUser : IUserConfirmation<ApplicationUser>
    {
        public Task<bool> IsConfirmedAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
            => Task.FromResult(true);
    }

    private sealed class RecordingAuditService : IAuditService
    {
        public List<AuditEntry> Entries { get; } = [];

        public Task RecordAsync(
            string action,
            string entityName,
            string? entityId = null,
            object? before = null,
            object? after = null,
            CancellationToken cancellationToken = default)
        {
            var path = after?.GetType().GetProperty("Path")?.GetValue(after)?.ToString();
            Entries.Add(new AuditEntry(action, entityName, entityId, path));
            return Task.CompletedTask;
        }
    }

    private sealed record AuditEntry(string Action, string EntityName, string? EntityId, string? After);
}
