using Craftsman.App.Services;
using Craftsman.Domain.Production.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Craftsman.App.Controllers;

public sealed class ProductionController : Controller
{
    private readonly ProductionScheduleService productionScheduleService;

    public ProductionController(ProductionScheduleService productionScheduleService)
    {
        this.productionScheduleService = productionScheduleService;
    }

    public async Task<IActionResult> Index(ProductionTaskStatus? status, DateOnly? plannedDate, CancellationToken cancellationToken)
    {
        ViewBag.Status = status;
        ViewBag.PlannedDate = plannedDate;

        var tasks = await productionScheduleService.ListAsync(status, plannedDate, cancellationToken);
        return View(tasks);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Advance(Guid id, string action, CancellationToken cancellationToken)
    {
        await productionScheduleService.AdvanceAsync(id, action, cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
