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
    private readonly TimeProvider timeProvider;

    public ProductionController(
        ProductionScheduleService productionScheduleService,
        IAuditService auditService,
        TimeProvider timeProvider)
    {
        this.productionScheduleService = productionScheduleService;
        this.auditService = auditService;
        this.timeProvider = timeProvider;
    }

    public async Task<IActionResult> Index(ProductionTaskStatus? status, DateOnly? plannedDate, CancellationToken cancellationToken)
    {
        ViewBag.Status = status;
        ViewBag.PlannedDate = plannedDate;
        var currentDate = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var referenceDate = plannedDate?.ToDateTime(TimeOnly.MinValue) ?? currentDate.ToDateTime(TimeOnly.MinValue);
        ViewBag.ReferenceDate = referenceDate;
        ViewBag.Today = currentDate;

        var tasks = await productionScheduleService.ListAsync(status, plannedDate ?? DateOnly.FromDateTime(referenceDate), cancellationToken);
        return View(tasks);
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult CurrentMonth()
    {
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        return Json(new { year = today.Year, month = today.Month });
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Calendar(ProductionTaskStatus? status, CancellationToken cancellationToken)
    {
        var currentDate = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        ViewBag.Status = status;
        ViewBag.PlannedDate = null;
        ViewBag.ReferenceDate = currentDate.ToDateTime(TimeOnly.MinValue);
        ViewBag.Today = currentDate;

        var tasks = await productionScheduleService.ListAsync(status, plannedDate: currentDate, cancellationToken);
        return PartialView("_Calendar", tasks);
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
