using Craftsman.App.Services;
using Microsoft.AspNetCore.Mvc;

namespace Craftsman.App.Controllers;

public sealed class OrdersController : Controller
{
    private readonly OrderQueryService orderQueryService;

    public OrdersController(OrderQueryService orderQueryService)
    {
        this.orderQueryService = orderQueryService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var orders = await orderQueryService.ListAsync(cancellationToken);
        return View(orders);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var order = await orderQueryService.GetDetailAsync(id, cancellationToken);

        return order is null ? NotFound() : View(order);
    }
}
