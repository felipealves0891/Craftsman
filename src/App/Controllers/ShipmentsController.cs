using Craftsman.App.Models;
using Craftsman.App.Services;
using Craftsman.Domain.Shipping.Entities;
using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Craftsman.App.Controllers;

[Authorize(Policy = ApplicationPolicies.Read)]
public sealed class ShipmentsController : Controller
{
    private readonly ShippingAppService shippingAppService;
    private readonly IAuditService auditService;

    public ShipmentsController(ShippingAppService shippingAppService, IAuditService auditService)
    {
        this.shippingAppService = shippingAppService;
        this.auditService = auditService;
    }

    public async Task<IActionResult> Index(ShipmentStatus? status, CancellationToken cancellationToken)
    {
        ViewBag.Status = status;
        var shipments = await shippingAppService.ListAsync(status, cancellationToken);
        return View(shipments);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? orderId, CancellationToken cancellationToken)
    {
        var model = await shippingAppService.BuildCreateModelAsync(orderId, cancellationToken: cancellationToken);
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.Write)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateShipmentInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            input = await shippingAppService.BuildCreateModelAsync(input: input, cancellationToken: cancellationToken);
            return View(input);
        }

        try
        {
            await shippingAppService.CreateAsync(input, cancellationToken);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(nameof(CreateShipmentInputModel.OrderId), exception.Message);
            input = await shippingAppService.BuildCreateModelAsync(input: input, cancellationToken: cancellationToken);
            return View(input);
        }
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.Write)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, ShipmentStatus status, CancellationToken cancellationToken)
    {
        try
        {
            await shippingAppService.UpdateStatusAsync(id, status, cancellationToken);
            await auditService.RecordAsync(
                AuditAction.ShipmentStatusUpdated,
                "Shipment",
                id.ToString(),
                after: new { Status = status.ToString() },
                cancellationToken: cancellationToken);
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
