using System.Text.Json;
using Craftsman.App.Models;
using Craftsman.App.Services;
using Craftsman.Domain;
using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Craftsman.App.Controllers;

[Authorize(Policy = ApplicationPolicies.Read)]
public sealed class ManualOrdersController : Controller
{
    private readonly ILogger<ManualOrdersController> _logger;
    private readonly ManualOrderService manualOrderService;
    private readonly OrderQueryService orderQueryService;

    public ManualOrdersController(
        ILogger<ManualOrdersController> logger,
        ManualOrderService manualOrderService, 
        OrderQueryService orderQueryService)
    {
        this._logger = logger;
        this.manualOrderService = manualOrderService;
        this.orderQueryService = orderQueryService;
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await PopulateFormOptionsAsync(cancellationToken);
        return View(new ManualOrderInputModel());
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.Write)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ManualOrderInputModel input, CancellationToken cancellationToken)
    {
        using(var log = CreateLog(input))
        {
            var data = log.AddStep($"{nameof(ManualOrdersController)}.{nameof(Create)}");
            data["ModelState.IsValid"] = ModelState.IsValid;

            if (!ModelState.IsValid)
            {
                data["ModelState.Errors"] = ModelState
                    .Where(x => x.Value is not null && x.Value.Errors.Any())
                    .ToDictionary(x => x.Key, x => x.Value);

                var fields = string.Join(", ", ModelState
                                                    .Where(x => x.Value is not null && x.Value.Errors.Any())
                                                    .Select(x => x.Key));

                TempData["Error"] = $"Preencha todos os campos obrigatorios! \r\n {fields}";
                await PopulateFormOptionsAsync(cancellationToken);
                return View(input);
            }

            try
            {
                var orderId = await manualOrderService.CreateAsync(input, log, cancellationToken);
                log.FinishLogWithSuccess(orderId);

                TempData["Success"] = "Pedido criado com sucesso!";
                return RedirectToAction("Details", "Orders", new { id = orderId });
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                TempData["Error"] = "Erro inesperado, tente novamente mais tarde!";
                log.FinishLogWithError(exception);
                ModelState.AddModelError(string.Empty, exception.Message);
                await PopulateFormOptionsAsync(cancellationToken);
                return View(input);
            }    
        }
    }

    private LogData CreateLog(object body)
    {
        return new LogData(
            Request.Method,
            Request.Path,
            Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString()),
            body);
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var input = await manualOrderService.GetInputAsync(id, cancellationToken);
        if (input is null)
        {
            return NotFound();
        }

        await PopulateFormOptionsAsync(cancellationToken);
        return View("Create", input);
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.Write)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ManualOrderInputModel input, CancellationToken cancellationToken)
    {
        input.Id = id;

        if (!ModelState.IsValid)
        {
            await PopulateFormOptionsAsync(cancellationToken);
            return View("Create", input);
        }

        try
        {
            await manualOrderService.UpdateAsync(id, input, cancellationToken);
            return RedirectToAction("Details", "Orders", new { id });
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await PopulateFormOptionsAsync(cancellationToken);
            return View("Create", input);
        }
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.Write)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSource(OrderSourceInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Informe o nome da origem.";
            return RedirectToAction(nameof(Create));
        }

        try
        {
            await manualOrderService.CreateOrderSourceAsync(input, cancellationToken);
            TempData["Success"] = "Origem cadastrada.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Create));
    }

    public async Task<IActionResult> LinkItems(Guid id, CancellationToken cancellationToken)
    {
        var order = await orderQueryService.GetDetailAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        await PopulateFormOptionsAsync(cancellationToken);
        return View(order);
    }

    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.Write)]
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

    private async Task PopulateFormOptionsAsync(CancellationToken cancellationToken)
    {
        var products = await manualOrderService.ListProductOptionsAsync(cancellationToken);
        ViewBag.Products = products.Select(product => new SelectListItem(product.Name, product.Id.ToString())).ToList();

        var sources = await manualOrderService.ListOrderSourcesAsync(cancellationToken);
        ViewBag.OrderSources = sources.Select(source => new SelectListItem(source, source)).ToList();
    }
}
