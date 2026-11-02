using Craftsman.App.Controllers;
using Craftsman.App.Models;
using Craftsman.App.Services;
using Craftsman.Domain.Events;
using Craftsman.Domain.Finance.Services;
using Craftsman.Domain.Inventory.Services;
using Craftsman.Domain.ProductCatalog.Services;
using Craftsman.Domain.Production.Services;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Craftsman.Tests.Application;

public sealed class ManualOrdersControllerCoverageTests
{
    [Fact]
    public async Task Manual_orders_controller_handles_form_validation_and_service_errors()
    {
        await using var dbContext = CreateDbContext();
        await new OrderSourceCatalogRepository(dbContext).AddAsync(new OrderSource(Guid.NewGuid(), "Manual"));
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext);

        Assert.IsType<ViewResult>(await controller.Create(CancellationToken.None));

        controller.ModelState.AddModelError("Source", "required");
        Assert.IsType<ViewResult>(await controller.Create(new ManualOrderInputModel(), CancellationToken.None));

        controller.ModelState.Clear();
        var invalidSource = Assert.IsType<ViewResult>(await controller.Create(
            new ManualOrderInputModel
            {
                Source = "Unknown",
                ShippingDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10),
                Items = [new ManualOrderItemInputModel { Description = "Item", Quantity = 1, UnitPriceAmount = 10 }]
            },
            CancellationToken.None));
        Assert.Contains(controller.ModelState.Values.SelectMany(value => value.Errors), error => error.ErrorMessage.Contains("Origem"));
        Assert.NotNull(invalidSource.Model);

        var existingOrder = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", "CTRL-EDIT-001"),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Item", 1, new Money(10, "BRL"))]);
        await new OrderRepository(dbContext).AddAsync(existingOrder);
        await dbContext.SaveChangesAsync();
        Assert.IsType<ViewResult>(await controller.Edit(existingOrder.Id, CancellationToken.None));
        Assert.IsType<NotFoundResult>(await controller.Edit(Guid.NewGuid(), CancellationToken.None));
        controller.ModelState.Clear();
        controller.ModelState.AddModelError("Source", "required");
        Assert.IsType<ViewResult>(await controller.Edit(Guid.NewGuid(), new ManualOrderInputModel(), CancellationToken.None));

        controller.ModelState.Clear();
        controller.ModelState.AddModelError("Name", "required");
        var invalidSourceRedirect = Assert.IsType<RedirectToActionResult>(await controller.CreateSource(new OrderSourceInputModel(), CancellationToken.None));
        Assert.Equal(nameof(ManualOrdersController.Create), invalidSourceRedirect.ActionName);

        controller.ModelState.Clear();
        var duplicateSourceRedirect = Assert.IsType<RedirectToActionResult>(await controller.CreateSource(new OrderSourceInputModel { Name = "Manual" }, CancellationToken.None));
        Assert.Equal(nameof(ManualOrdersController.Create), duplicateSourceRedirect.ActionName);
        Assert.Contains("ja existe", controller.TempData["Error"]?.ToString());

        Assert.IsType<NotFoundResult>(await controller.LinkItems(Guid.NewGuid(), CancellationToken.None));
        var linkRedirect = Assert.IsType<RedirectToActionResult>(await controller.LinkItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
        Assert.Equal(nameof(ManualOrdersController.LinkItems), linkRedirect.ActionName);
    }

    private static ManualOrdersController CreateController(AppDbContext dbContext)
    {
        var orderRepository = new OrderRepository(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var stockMovementRepository = new StockMovementRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);
        var shipmentRepository = new ShipmentRepository(dbContext);
        var manualOrderService = new ManualOrderService(
            orderRepository,
            new OrderSourceCatalogRepository(dbContext),
            productRepository,
            productionTaskRepository,
            stockMovementRepository,
            shipmentRepository,
            new FinancialSettlementRepository(dbContext),
            new OrderDeletionProcessRepository(dbContext),
            new OrderProductionPlanningService(
                orderRepository,
                new ProductionPlanner(productRepository, new InventoryService(new RawMaterialRepository(dbContext), stockMovementRepository)),
                productionTaskRepository,
                unitOfWork,
                new NoOpDomainEventPublisher(),
                NullLogger<OrderProductionPlanningService>.Instance),
            unitOfWork,
            new NoOpDomainEventPublisher(),
            new StaticCurrentUserContext(CurrentUserInfo.Anonymous()));
        var orderQueryService = new OrderQueryService(orderRepository, productRepository, productionTaskRepository, shipmentRepository);

        return new ManualOrdersController(
            NullLogger<ManualOrdersController>.Instance,
            manualOrderService,
            orderQueryService)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), new EmptyTempDataProvider())
        };
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class EmptyTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private sealed class NoOpDomainEventPublisher : IDomainEventPublisher
    {
        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent => Task.CompletedTask;
    }
}
