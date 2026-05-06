using Craftsman.App.Services;
using Microsoft.AspNetCore.Mvc;

namespace Craftsman.App.ViewComponents;

public sealed class StockAlertsViewComponent : ViewComponent
{
    private readonly StockAlertAppService stockAlertAppService;

    public StockAlertsViewComponent(StockAlertAppService stockAlertAppService)
    {
        this.stockAlertAppService = stockAlertAppService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var alerts = await stockAlertAppService.ListAlertsAsync(HttpContext.RequestAborted);
        return View(alerts);
    }
}
