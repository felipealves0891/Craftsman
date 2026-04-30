using Craftsman.App.Services;
using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Craftsman.App.Controllers;

[Authorize(Policy = ApplicationPolicies.Read)]
public sealed class FinanceController : Controller
{
    private readonly FinanceAppService financeAppService;

    public FinanceController(FinanceAppService financeAppService)
    {
        this.financeAppService = financeAppService;
    }

    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        ViewBag.From = from;
        ViewBag.To = to;

        var settlements = await financeAppService.ListAsync(from, to, cancellationToken);
        return View(settlements);
    }

    public async Task<IActionResult> Details(Guid orderId, CancellationToken cancellationToken)
    {
        var settlement = await financeAppService.GetByOrderIdAsync(orderId, cancellationToken);

        return settlement is null ? NotFound() : View(settlement);
    }
}
