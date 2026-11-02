using Craftsman.App.Controllers;
using System.Diagnostics;
using Craftsman.App.Services;
using Craftsman.Controllers;
using Craftsman.Domain.Events;
using Craftsman.Domain.Finance.Services;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Domain.Services;
using Craftsman.Domain.Shipping.Entities;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Craftsman.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Craftsman.Tests.Application;

public sealed class MvcControllerCoverageTests
{
    [Fact]
    public void Home_controller_returns_expected_views_and_error_request_id()
    {
        var controller = new HomeController(NullLogger<HomeController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        Assert.IsType<ViewResult>(controller.Index());
        Assert.IsType<ViewResult>(controller.Privacy());

        var error = Assert.IsType<ViewResult>(controller.Error());
        var model = Assert.IsType<ErrorViewModel>(error.Model);
        Assert.Equal(controller.HttpContext.TraceIdentifier, model.RequestId);

        using var activity = new Activity("home-error-test").Start();
        var activityError = Assert.IsType<ViewResult>(controller.Error());
        Assert.Equal(activity.Id, Assert.IsType<ErrorViewModel>(activityError.Model).RequestId);
    }

    [Fact]
    public void Import_controller_redirects_both_actions_to_orders()
    {
        var controller = new ImportController();

        var get = Assert.IsType<RedirectToActionResult>(controller.Index());
        Assert.Equal("Index", get.ActionName);
        Assert.Equal("Orders", get.ControllerName);

        var post = Assert.IsType<RedirectToActionResult>(controller.Run());
        Assert.Equal("Index", post.ActionName);
        Assert.Equal("Orders", post.ControllerName);
    }

    [Fact]
    public async Task Finance_controller_renders_lists_and_handles_manual_generation_results()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedDeliveredOrderAsync(dbContext);
        var controller = new FinanceController(CreateFinanceService(dbContext))
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new EmptyTempDataProvider())
        };

        var from = new DateOnly(2031, 6, 1);
        var to = new DateOnly(2031, 6, 30);
        var index = Assert.IsType<ViewResult>(await controller.Index(from, to, CancellationToken.None));
        Assert.Equal(from, controller.ViewBag.From);
        Assert.Equal(to, controller.ViewBag.To);
        Assert.NotNull(index.Model);

        var pending = Assert.IsType<ViewResult>(await controller.Pending(CancellationToken.None));
        Assert.Single(Assert.IsAssignableFrom<IReadOnlyCollection<Craftsman.App.Models.FinancialSettlementPendingViewModel>>(pending.Model));

        Assert.IsType<NotFoundResult>(await controller.Details(Guid.NewGuid(), CancellationToken.None));

        var generate = Assert.IsType<RedirectToActionResult>(await controller.Generate(order.Id, CancellationToken.None));
        Assert.Equal(nameof(FinanceController.Details), generate.ActionName);
        Assert.Equal("Apuracao financeira gerada.", controller.TempData["Success"]);

        var details = Assert.IsType<ViewResult>(await controller.Details(order.Id, CancellationToken.None));
        Assert.NotNull(details.Model);

        var missingOrder = Assert.IsType<RedirectToActionResult>(await controller.Generate(Guid.NewGuid(), CancellationToken.None));
        Assert.Equal(nameof(FinanceController.Pending), missingOrder.ActionName);
        Assert.Equal("Order was not found.", controller.TempData["Error"]);
    }

    private static FinanceAppService CreateFinanceService(AppDbContext dbContext)
    {
        var settlementRepository = new FinancialSettlementRepository(dbContext);
        var shipmentRepository = new ShipmentRepository(dbContext);
        var orderRepository = new OrderRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);

        return new FinanceAppService(
            settlementRepository,
            new SettlementCalculator(orderRepository, productionTaskRepository, stockMovementRepository, shipmentRepository),
            shipmentRepository,
            orderRepository,
            new UnitOfWork(dbContext),
            new NoOpDomainEventPublisher());
    }

    private static async Task<Order> SeedDeliveredOrderAsync(AppDbContext dbContext)
    {
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "CTRL-FIN-1"),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Bolsa", 2, new Money(25, "BRL"))]);

        await new OrderRepository(dbContext).AddAsync(order);
        await new ShipmentRepository(dbContext).AddAsync(new Shipment(
            Guid.NewGuid(), order.Id, "TRACK-1", ShipmentStatus.Delivered, deliveredAt: DateTimeOffset.UtcNow));
        await dbContext.SaveChangesAsync();
        return order;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class NoOpDomainEventPublisher : IDomainEventPublisher
    {
        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent => Task.CompletedTask;
    }

    private sealed class EmptyTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
