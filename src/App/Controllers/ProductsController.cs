using Craftsman.App.Models;
using Craftsman.App.Services;
using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Craftsman.App.Controllers;

[Authorize(Policy = ApplicationPolicies.Read)]
public sealed class ProductsController : Controller
{
    private readonly ProductCatalogAppService productCatalogAppService;

    public ProductsController(ProductCatalogAppService productCatalogAppService)
    {
        this.productCatalogAppService = productCatalogAppService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var products = await productCatalogAppService.ListProductsAsync(cancellationToken);
        return View(products);
    }

    public IActionResult Create()
    {
        return View("Edit", new ProductInputModel());
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var product = await productCatalogAppService.GetProductInputAsync(id, cancellationToken);
        return product is null ? NotFound() : View(product);
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.Write)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        try
        {
            await productCatalogAppService.SaveProductAsync(input, cancellationToken);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(input);
        }
    }

    public async Task<IActionResult> BillOfMaterials(Guid id, CancellationToken cancellationToken)
    {
        var model = await productCatalogAppService.GetBillOfMaterialsInputAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        await PopulateRawMaterialsAsync(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.Write)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BillOfMaterials(BillOfMaterialsInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateRawMaterialsAsync(cancellationToken);
            return View(input);
        }

        try
        {
            await productCatalogAppService.SaveBillOfMaterialsAsync(input, cancellationToken);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await PopulateRawMaterialsAsync(cancellationToken);
            return View(input);
        }
    }

    public async Task<IActionResult> Mappings(CancellationToken cancellationToken)
    {
        await PopulateProductsAsync(cancellationToken);
        ViewBag.Mappings = await productCatalogAppService.ListMappingsAsync(cancellationToken);
        return View(new ProductMappingInputModel());
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.Write)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Mappings(ProductMappingInputModel input, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await productCatalogAppService.CreateMappingAsync(input, cancellationToken);
                return RedirectToAction(nameof(Mappings));
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                ModelState.AddModelError(string.Empty, exception.Message);
            }
        }

        await PopulateProductsAsync(cancellationToken);
        ViewBag.Mappings = await productCatalogAppService.ListMappingsAsync(cancellationToken);
        return View(input);
    }

    private async Task PopulateProductsAsync(CancellationToken cancellationToken)
    {
        var products = await productCatalogAppService.ListProductOptionsAsync(cancellationToken);
        ViewBag.Products = products.Select(product => new SelectListItem(product.Name, product.Id.ToString())).ToList();
    }

    private async Task PopulateRawMaterialsAsync(CancellationToken cancellationToken)
    {
        var materials = await productCatalogAppService.ListRawMaterialOptionsAsync(cancellationToken);
        ViewBag.RawMaterials = materials
            .Select(material => new SelectListItem($"{material.Name} ({material.UnitOfMeasure})", material.Id.ToString()))
            .ToList();
    }
}
