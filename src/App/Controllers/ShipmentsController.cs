using Craftsman.App.Models;
using Craftsman.App.Services;
using Craftsman.Domain.Shipping.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Craftsman.App.Controllers;

public sealed class ShipmentsController : Controller
{
    private readonly ShippingAppService shippingAppService;

    public ShipmentsController(ShippingAppService shippingAppService)
    {
        this.shippingAppService = shippingAppService;
    }

    public async Task<IActionResult> Index(ShipmentStatus? status, CancellationToken cancellationToken)
    {
        ViewBag.Status = status;
        var shipments = await shippingAppService.ListAsync(status, cancellationToken);
        return View(shipments);
    }

    [HttpGet]
    public IActionResult Create(Guid? orderId)
    {
        return View(new CreateShipmentInputModel { OrderId = orderId ?? Guid.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateShipmentInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        await shippingAppService.CreateAsync(input, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, ShipmentStatus status, CancellationToken cancellationToken)
    {
        await shippingAppService.UpdateStatusAsync(id, status, cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
