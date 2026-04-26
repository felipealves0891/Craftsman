using Craftsman.App.Models;
using Craftsman.App.Services;
using Craftsman.Domain.Integration.Services;
using Microsoft.AspNetCore.Mvc;

namespace Craftsman.App.Controllers;

public sealed class OrdersController : Controller
{
    private readonly IOrderImportPipeline orderImportPipeline;
    private readonly OrderQueryService orderQueryService;

    public OrdersController(OrderQueryService orderQueryService, IOrderImportPipeline orderImportPipeline)
    {
        this.orderQueryService = orderQueryService;
        this.orderImportPipeline = orderImportPipeline;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var orders = await orderQueryService.ListAsync(cancellationToken);
        return View(new OrdersIndexViewModel(orders, null));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(CancellationToken cancellationToken)
    {
        var result = await orderImportPipeline.ImportAsync(cancellationToken);
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
}
