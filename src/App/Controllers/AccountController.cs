using Craftsman.App.Models;
using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Craftsman.App.Controllers;

[AllowAnonymous]
public sealed class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly IAuditService auditService;

    public AccountController(SignInManager<ApplicationUser> signInManager, IAuditService auditService)
    {
        this.signInManager = signInManager;
        this.auditService = auditService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginInputModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var result = await signInManager.PasswordSignInAsync(input.Email, input.Password, input.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            await auditService.RecordAsync(AuditAction.LoginSucceeded, "Authentication", input.Email, after: new { input.Email }, cancellationToken: cancellationToken);
            return LocalRedirect(Url.IsLocalUrl(input.ReturnUrl) ? input.ReturnUrl! : Url.Action("Index", "Home")!);
        }

        await auditService.RecordAsync(AuditAction.LoginFailed, "Authentication", input.Email, after: new { input.Email }, cancellationToken: cancellationToken);
        ModelState.AddModelError(string.Empty, "Login invalido.");
        return View(input);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userName = User.Identity?.Name;
        await signInManager.SignOutAsync();
        await auditService.RecordAsync(AuditAction.Logout, "Authentication", userName, before: new { UserName = userName }, cancellationToken: cancellationToken);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> AccessDenied(CancellationToken cancellationToken)
    {
        await auditService.RecordAsync(AuditAction.AccessDenied, "Authorization", User.Identity?.Name, after: new { Path = HttpContext.Request.Path.Value }, cancellationToken: cancellationToken);
        return View();
    }
}
