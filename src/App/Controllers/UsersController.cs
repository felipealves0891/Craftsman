using Craftsman.App.Models;
using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.App.Controllers;

[Authorize(Policy = ApplicationPolicies.AdminOnly)]
public sealed class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IAuditService auditService;

    public UsersController(UserManager<ApplicationUser> userManager, IAuditService auditService)
    {
        this.userManager = userManager;
        this.auditService = auditService;
    }

    public async Task<IActionResult> Index()
    {
        var users = await userManager.Users.OrderBy(user => user.Email).ToListAsync();
        var model = new List<UserListItemViewModel>();
        foreach (var user in users)
        {
            model.Add(new UserListItemViewModel(user.Id, user.Email ?? user.UserName ?? user.Id.ToString(), (await userManager.GetRolesAsync(user)).ToArray()));
        }

        return View(model);
    }

    public IActionResult Create()
    {
        PopulateRoles();
        return View(new CreateUserInputModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            PopulateRoles();
            return View(input);
        }

        var user = new ApplicationUser { UserName = input.Email, Email = input.Email, EmailConfirmed = true };
        var createResult = await userManager.CreateAsync(user, input.Password);
        if (createResult.Succeeded)
        {
            var roleResult = await userManager.AddToRoleAsync(user, input.Role);
            if (roleResult.Succeeded)
            {
                await auditService.RecordAsync(AuditAction.UserRoleChanged, nameof(ApplicationUser), user.Id.ToString(), after: new { user.Email, input.Role }, cancellationToken: cancellationToken);
                return RedirectToAction(nameof(Index));
            }

            AddErrors(roleResult);
        }
        else
        {
            AddErrors(createResult);
        }

        PopulateRoles();
        return View(input);
    }

    private void PopulateRoles()
    {
        ViewBag.Roles = new[]
        {
            new SelectListItem(ApplicationRoles.Admin, ApplicationRoles.Admin),
            new SelectListItem(ApplicationRoles.Operador, ApplicationRoles.Operador),
            new SelectListItem(ApplicationRoles.Consulta, ApplicationRoles.Consulta)
        };
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
