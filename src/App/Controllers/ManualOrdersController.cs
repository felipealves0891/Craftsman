using Craftsman.App.Models;
using Craftsman.App.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Craftsman.App.Controllers;

public sealed class ManualOrdersController : Controller
{
    private readonly ManualOrderService manualOrderService;
    private readonly OrderQueryService orderQueryService;

    public ManualOrdersController(ManualOrderService manualOrderService, OrderQueryService orderQueryService)
    {
        this.manualOrderService = manualOrderService;
        this.orderQueryService = orderQueryService;
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await PopulateProductsAsync(cancellationToken);
        return View(new ManualOrderInputModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ManualOrderInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateProductsAsync(cancellationToken);
            return View(input);
        }

        try
        {
            var orderId = await manualOrderService.CreateAsync(input, cancellationToken);
            return RedirectToAction("Details", "Orders", new { id = orderId });
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await PopulateProductsAsync(cancellationToken);
            return View(input);
        }
    }

    public async Task<IActionResult> LinkItems(Guid id, CancellationToken cancellationToken)
    {
        var order = await orderQueryService.GetDetailAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        await PopulateProductsAsync(cancellationToken);
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkItem(Guid orderId, Guid itemId, Guid productId, CancellationToken cancellationToken)
    {
        try
        {
            await manualOrderService.LinkProductAsync(orderId, itemId, productId, cancellationToken);
            return RedirectToAction(nameof(LinkItems), new { id = orderId });
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(LinkItems), new { id = orderId });
        }
    }

    private async Task PopulateProductsAsync(CancellationToken cancellationToken)
    {
        var products = await manualOrderService.ListProductOptionsAsync(cancellationToken);
        ViewBag.Products = products.Select(product => new SelectListItem(product.Name, product.Id.ToString())).ToList();
    }
}
