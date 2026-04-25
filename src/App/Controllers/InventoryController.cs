using Craftsman.App.Models;
using Craftsman.App.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Craftsman.App.Controllers;

public sealed class InventoryController : Controller
{
    private readonly InventoryAppService inventoryAppService;

    public InventoryController(InventoryAppService inventoryAppService)
    {
        this.inventoryAppService = inventoryAppService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var materials = await inventoryAppService.ListRawMaterialsAsync(cancellationToken);
        return View(materials);
    }

    public IActionResult CreateMaterial()
    {
        return View("EditMaterial", new RawMaterialInputModel());
    }

    public async Task<IActionResult> EditMaterial(Guid id, CancellationToken cancellationToken)
    {
        var material = await inventoryAppService.GetRawMaterialInputAsync(id, cancellationToken);
        return material is null ? NotFound() : View(material);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMaterial(RawMaterialInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        try
        {
            await inventoryAppService.SaveRawMaterialAsync(input, cancellationToken);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(input);
        }
    }

    public async Task<IActionResult> Movements(CancellationToken cancellationToken)
    {
        await PopulateRawMaterialsAsync(cancellationToken);
        ViewBag.Movements = await inventoryAppService.ListMovementsAsync(cancellationToken: cancellationToken);
        return View(new StockMovementInputModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Movements(StockMovementInputModel input, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await inventoryAppService.RegisterMovementAsync(input, cancellationToken);
                return RedirectToAction(nameof(Movements));
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                ModelState.AddModelError(string.Empty, exception.Message);
            }
        }

        await PopulateRawMaterialsAsync(cancellationToken);
        ViewBag.Movements = await inventoryAppService.ListMovementsAsync(cancellationToken: cancellationToken);
        return View(input);
    }

    public async Task<IActionResult> Balances(CancellationToken cancellationToken)
    {
        var balances = await inventoryAppService.ListBalancesAsync(cancellationToken);
        return View(balances);
    }

    public async Task<IActionResult> History(Guid id, CancellationToken cancellationToken)
    {
        var movements = await inventoryAppService.ListMovementsAsync(id, cancellationToken);
        return View(movements);
    }

    private async Task PopulateRawMaterialsAsync(CancellationToken cancellationToken)
    {
        var materials = await inventoryAppService.ListRawMaterialOptionsAsync(cancellationToken);
        ViewBag.RawMaterials = materials
            .Select(material => new SelectListItem($"{material.Name} ({material.UnitOfMeasure})", material.Id.ToString()))
            .ToList();
    }
}
