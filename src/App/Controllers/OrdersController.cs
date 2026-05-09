using Craftsman.App.Models;
using Craftsman.App.Services;
using Craftsman.Domain.Integration.Services;
using Craftsman.Domain.Production.Services;
using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Craftsman.App.Controllers;

[Authorize(Policy = ApplicationPolicies.Read)]
public sealed class OrdersController : Controller
{
    private readonly IOrderImportPipeline orderImportPipeline;
    private readonly OrderQueryService orderQueryService;
    private readonly OrderProductionPlanningService productionPlanningService;
    private readonly StockAlertAppService stockAlertAppService;
    private readonly IAuditService auditService;

    public OrdersController(
        OrderQueryService orderQueryService,
        IOrderImportPipeline orderImportPipeline,
        OrderProductionPlanningService productionPlanningService,
        StockAlertAppService stockAlertAppService,
        IAuditService auditService)
    {
        this.orderQueryService = orderQueryService;
        this.orderImportPipeline = orderImportPipeline;
        this.productionPlanningService = productionPlanningService;
        this.stockAlertAppService = stockAlertAppService;
        this.auditService = auditService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var orders = await orderQueryService.ListAsync(cancellationToken);
        return View(new OrdersIndexViewModel(orders, null));
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.Write)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(CancellationToken cancellationToken)
    {
        var result = await orderImportPipeline.ImportAsync(cancellationToken);
        await auditService.RecordAsync(
            AuditAction.ManualImport,
            "OrderImport",
            after: new { result.ImportedCount, result.SkippedCount, FailureCount = result.Failures.Count },
            cancellationToken: cancellationToken);
        var orders = await orderQueryService.ListAsync(cancellationToken);
        var importResult = new ImportResultViewModel(
            result.ImportedCount,
            result.SkippedCount,
            result.Failures.Select(failure => $"{failure.Source}/{failure.ExternalOrderId}: {failure.Reason}").ToList().AsReadOnly());

        return View("Index", new OrdersIndexViewModel(orders, importResult));
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var order = await orderQueryService.GetDetailAsync(id, cancellationToken);

        return order is null ? NotFound() : View(order);
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.Write)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendToProduction(Guid id, CancellationToken cancellationToken)
    {
        var stockAlerts = await stockAlertAppService.ListAlertsForOrderAsync(id, cancellationToken);
        var planned = await productionPlanningService.TryPlanAsync(id, cancellationToken);

        await auditService.RecordAsync(
            AuditAction.SendToProduction,
            "Order",
            id.ToString(),
            after: new { Planned = planned },
            cancellationToken: cancellationToken);

        TempData[planned ? "Success" : "Error"] = planned
            ? "Pedido enviado para producao."
            : "Pedido nao pode ser enviado para producao. Verifique vinculos, estoque e tarefas ja existentes.";
            
        if (stockAlerts.Count > 0)
        {
            TempData["StockAlerts"] = string.Join(
                " | ",
                stockAlerts.Select(alert => $"{alert.LevelLabel}: {alert.RawMaterialName} com saldo {alert.Balance} {alert.UnitOfMeasure} (limite {alert.ReachedLimit})"));
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
