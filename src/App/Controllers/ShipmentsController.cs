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
    public IActionResult Create(Guid? orderId)
    {
        return View(new CreateShipmentInputModel { OrderId = orderId ?? Guid.Empty });
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.Write)]
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
    [Authorize(Policy = ApplicationPolicies.Write)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, ShipmentStatus status, CancellationToken cancellationToken)
    {
        await shippingAppService.UpdateStatusAsync(id, status, cancellationToken);
        await auditService.RecordAsync(
            AuditAction.ShipmentStatusUpdated,
            "Shipment",
            id.ToString(),
            after: new { Status = status.ToString() },
            cancellationToken: cancellationToken);
        return RedirectToAction(nameof(Index));
    }
}
