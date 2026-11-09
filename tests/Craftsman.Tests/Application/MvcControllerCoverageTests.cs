using Craftsman.App.Controllers;
using Craftsman.App.Models;
using System.Diagnostics;
using Craftsman.App.Services;
using Craftsman.Controllers;
using Craftsman.Domain.Events;
using Craftsman.Domain.Finance.Services;
using Craftsman.Domain.Integration.Services;
using Craftsman.Domain.Inventory.Services;
using Craftsman.Domain.Production.Services;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Domain.Services;
using Craftsman.Domain.Shipping.Entities;
using Craftsman.Domain.Shipping.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Craftsman.Infra.Security;
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

    [Fact]
    public async Task Shipments_controller_handles_empty_create_and_update_failures()
    {
        await using var dbContext = CreateDbContext();
        var shipmentRepository = new ShipmentRepository(dbContext);
        var orderRepository = new OrderRepository(dbContext);
        var orderQueryService = new OrderQueryService(
            orderRepository,
            new ProductRepository(dbContext),
            new ProductionTaskRepository(dbContext),
            shipmentRepository);
        var shippingService = new ShippingAppService(
            shipmentRepository,
            orderQueryService,
            new ShippingService(
                shipmentRepository,
                orderRepository,
                new UnitOfWork(dbContext),
                new NoOpDomainEventPublisher()));
        var controller = new ShipmentsController(shippingService, new NoOpAuditService())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), new EmptyTempDataProvider())
        };

        Assert.IsType<ViewResult>(await controller.Index(null, CancellationToken.None));
        Assert.IsType<ViewResult>(await controller.Create(Guid.NewGuid(), CancellationToken.None));

        controller.ModelState.AddModelError("OrderId", "required");
        var invalidCreate = Assert.IsType<ViewResult>(await controller.Create(new CreateShipmentInputModel(), CancellationToken.None));
        Assert.NotNull(invalidCreate.Model);

        controller.ModelState.Clear();
        var unavailableCreate = Assert.IsType<ViewResult>(await controller.Create(
            new CreateShipmentInputModel { OrderId = Guid.NewGuid() },
            CancellationToken.None));
        Assert.Contains(controller.ModelState.Values.SelectMany(value => value.Errors), error => error.ErrorMessage.Contains("nao foi encontrado"));
        Assert.NotNull(unavailableCreate.Model);

        var update = Assert.IsType<RedirectToActionResult>(await controller.UpdateStatus(
            Guid.NewGuid(),
            ShipmentStatus.Delivered,
            CancellationToken.None));
        Assert.Equal(nameof(ShipmentsController.Index), update.ActionName);
        Assert.NotNull(controller.TempData["Error"]);
    }

    [Fact]
    public async Task Orders_controller_covers_import_details_and_failure_redirects()
    {
        await using var dbContext = CreateDbContext();
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "CTRL-ORDERS-1"),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Produto", 1, new Money(10, "BRL"))]);
        await new OrderRepository(dbContext).AddAsync(order);
        await dbContext.SaveChangesAsync();

        var controller = CreateOrdersController(dbContext);

        var index = Assert.IsType<ViewResult>(await controller.Index(CancellationToken.None));
        Assert.IsType<OrdersIndexViewModel>(index.Model);

        var import = Assert.IsType<ViewResult>(await controller.Import(CancellationToken.None));
        var importModel = Assert.IsType<OrdersIndexViewModel>(import.Model);
        Assert.Equal("Index", import.ViewName);
        Assert.NotNull(importModel.LastImport);
        Assert.Equal(1, importModel.LastImport.ImportedCount);

        Assert.IsType<ViewResult>(await controller.Details(order.Id, CancellationToken.None));
        Assert.IsType<NotFoundResult>(await controller.Details(Guid.NewGuid(), CancellationToken.None));

        var send = Assert.IsType<RedirectToActionResult>(await controller.SendToProduction(order.Id, CancellationToken.None));
        Assert.Equal(nameof(OrdersController.Details), send.ActionName);
        Assert.NotNull(controller.TempData["Error"]);

        var revert = Assert.IsType<RedirectToActionResult>(await controller.RevertProduction(Guid.NewGuid(), CancellationToken.None));
        Assert.Equal(nameof(OrdersController.Details), revert.ActionName);
        Assert.NotNull(controller.TempData["Error"]);

        var noTransactionController = CreateOrdersController(dbContext, useTransaction: false);
        var noPlan = Assert.IsType<RedirectToActionResult>(await noTransactionController.RevertProduction(order.Id, CancellationToken.None));
        Assert.Equal(nameof(OrdersController.Details), noPlan.ActionName);
        Assert.Equal("O planejamento de producao ja estava revertido.", noTransactionController.TempData["Error"]);

        var delete = Assert.IsType<RedirectToActionResult>(await controller.Delete(Guid.NewGuid(), null, CancellationToken.None));
        Assert.Equal(nameof(OrdersController.Details), delete.ActionName);
        Assert.NotNull(controller.TempData["Error"]);

        var deleted = Assert.IsType<RedirectToActionResult>(await controller.Delete(order.Id, null, CancellationToken.None));
        Assert.Equal(nameof(OrdersController.Index), deleted.ActionName);
        Assert.Equal("Pedido excluido.", controller.TempData["Success"]);
    }

    private static OrdersController CreateOrdersController(AppDbContext dbContext, bool useTransaction = true)
    {
        var orderRepository = new OrderRepository(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var shipmentRepository = new ShipmentRepository(dbContext);
        var rawMaterialRepository = new RawMaterialRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);
        var planningService = new OrderProductionPlanningService(
            orderRepository,
            new ProductionPlanner(productRepository, new InventoryService(rawMaterialRepository, stockMovementRepository), productionTaskRepository),
            productionTaskRepository,
            unitOfWork,
            new NoOpDomainEventPublisher(),
            NullLogger<OrderProductionPlanningService>.Instance,
            stockMovementRepository,
            shipmentRepository);
        var manualOrderService = new ManualOrderService(
            orderRepository,
            new OrderSourceCatalogRepository(dbContext),
            productRepository,
            productionTaskRepository,
            stockMovementRepository,
            shipmentRepository,
            new FinancialSettlementRepository(dbContext),
            new OrderDeletionProcessRepository(dbContext),
            planningService,
            unitOfWork,
            new NoOpDomainEventPublisher(),
            new StaticCurrentUserContext(CurrentUserInfo.Anonymous()));

        return new OrdersController(
            new OrderQueryService(orderRepository, productRepository, productionTaskRepository, shipmentRepository),
            manualOrderService,
            new StubOrderImportPipeline(),
            planningService,
            new StockAlertAppService(rawMaterialRepository, stockMovementRepository, orderRepository, productRepository),
            new NoOpAuditService(),
            useTransaction ? unitOfWork : null)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), new EmptyTempDataProvider())
        };
    }

    private static FinanceAppService CreateFinanceService(AppDbContext dbContext)
    {
        var settlementRepository = new FinancialSettlementRepository(dbContext);
        var shipmentRepository = new ShipmentRepository(dbContext);
        var orderRepository = new OrderRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var productRepository = new ProductRepository(dbContext);

        return new FinanceAppService(
            settlementRepository,
            new SettlementCalculator(orderRepository, productionTaskRepository, stockMovementRepository, shipmentRepository, productRepository),
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

    private sealed class NoOpAuditService : IAuditService
    {
        public Task RecordAsync(
            string action,
            string entityName,
            string? entityId = null,
            object? before = null,
            object? after = null,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubOrderImportPipeline : IOrderImportPipeline
    {
        public Task<OrderImportResult> ImportAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new OrderImportResult(
                1,
                2,
                [new ImportFailure("Shopee", "EXT-1", "Falha de teste")]));
    }
}
