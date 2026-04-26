using Microsoft.AspNetCore.Mvc;

namespace Craftsman.App.Controllers;

public sealed class ImportController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Orders");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Run()
    {
        return RedirectToAction("Index", "Orders");
    }
}
