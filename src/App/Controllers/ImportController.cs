using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Craftsman.App.Controllers;

[Authorize(Policy = ApplicationPolicies.Read)]
public sealed class ImportController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Orders");
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.Write)]
    [ValidateAntiForgeryToken]
    public IActionResult Run()
    {
        return RedirectToAction("Index", "Orders");
    }
}
