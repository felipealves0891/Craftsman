using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Craftsman.App.Controllers;

[Authorize(Policy = ApplicationPolicies.Read)]
public sealed class DocsController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
