using Craftsman.App.Services;
using Craftsman.Domain.Production.Entities;
using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Craftsman.App.Controllers;

[Authorize(Policy = ApplicationPolicies.Read)]
public sealed class ProductionController : Controller
{
    private readonly ProductionScheduleService productionScheduleService;
    private readonly IAuditService auditService;

    public ProductionController(ProductionScheduleService productionScheduleService, IAuditService auditService)
    {
        this.productionScheduleService = productionScheduleService;
        this.auditService = auditService;
    }

    public async Task<IActionResult> Index(ProductionTaskStatus? status, DateOnly? plannedDate, CancellationToken cancellationToken)
    {
        ViewBag.Status = status;
        ViewBag.PlannedDate = plannedDate;

        var tasks = await productionScheduleService.ListAsync(status, plannedDate, cancellationToken);
        return View(tasks);
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.Write)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Advance(Guid id, string action, CancellationToken cancellationToken)
    {
        try
        {
            await productionScheduleService.AdvanceAsync(id, action, cancellationToken);
            await auditService.RecordAsync(
                AuditAction.ProductionAdvanced,
                "ProductionTask",
                id.ToString(),
                after: new { Action = action },
                cancellationToken: cancellationToken);
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
