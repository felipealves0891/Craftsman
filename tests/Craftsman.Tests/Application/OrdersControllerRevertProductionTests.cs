using Craftsman.App.Controllers;
using Craftsman.App.Services;
using Craftsman.Domain.Events;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Inventory.Services;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Production.Services;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Domain.Integration.Services;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Craftsman.Tests.Application;

public sealed class OrdersControllerRevertProductionTests
{
    [Fact]
    public async Task Revert_production_redirects_with_success_and_audits_the_operation()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext, OrderStatus.ReadyForProduction, ProductionTaskStatus.Planned);
        var audit = new RecordingAuditService();
        var controller = CreateController(dbContext, audit);

        var result = await controller.RevertProduction(order.Id, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(OrdersController.Details), redirect.ActionName);
        Assert.Equal("Planejamento de producao revertido.", controller.TempData["Success"]);
        Assert.Equal(AuditAction.ProductionPlanningReverted, audit.Action);
    }

    [Fact]
    public async Task Revert_production_reports_completed_task_rejection()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext, OrderStatus.InProduction, ProductionTaskStatus.Completed);
        var controller = CreateController(dbContext, new RecordingAuditService());

        var result = await controller.RevertProduction(order.Id, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Contains("completed", controller.TempData["Error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Revert_production_reports_already_reverted_order_without_new_effects()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext, OrderStatus.Normalized, taskStatus: null);
        var audit = new RecordingAuditService();
        var controller = CreateController(dbContext, audit);

        await controller.RevertProduction(order.Id, CancellationToken.None);

        Assert.Equal("O planejamento de producao ja estava revertido.", controller.TempData["Error"]);
        Assert.Equal(AuditAction.ProductionPlanningReverted, audit.Action);
    }

    [Fact]
    public async Task Revert_production_executes_audit_inside_transactional_unit_of_work()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext, OrderStatus.ReadyForProduction, ProductionTaskStatus.Planned);
        var transaction = new RecordingTransactionalUnitOfWork();
        var controller = CreateController(dbContext, new RecordingAuditService(), transaction);

        await controller.RevertProduction(order.Id, CancellationToken.None);

        Assert.True(transaction.Executed);
    }

    private static OrdersController CreateController(
        AppDbContext dbContext,
        IAuditService auditService,
        ITransactionalUnitOfWork? transactionalUnitOfWork = null)
    {
        var orderRepository = new OrderRepository(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var taskRepository = new ProductionTaskRepository(dbContext);
        var movementRepository = new StockMovementRepository(dbContext);
        var planningService = new OrderProductionPlanningService(
            orderRepository,
            new ProductionPlanner(
                productRepository,
                new InventoryService(new RawMaterialRepository(dbContext), movementRepository)),
            taskRepository,
            new UnitOfWork(dbContext),
            new NoOpDomainEventPublisher(),
            NullLogger<OrderProductionPlanningService>.Instance,
            movementRepository);

        return new OrdersController(
            new OrderQueryService(orderRepository, productRepository, taskRepository, new ShipmentRepository(dbContext)),
            null!,
            null!,
            planningService,
            null!,
            auditService,
            transactionalUnitOfWork)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new EmptyTempDataProvider())
        };
    }

    private static async Task<Order> SeedOrderAsync(
        AppDbContext dbContext,
        OrderStatus orderStatus,
        ProductionTaskStatus? taskStatus)
    {
        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "m");
        var product = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(material.Id, 2)]);
        var order = new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", Guid.NewGuid().ToString("N")),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Bolsa", 1, new Money(30, "BRL"), product.Id)],
            orderStatus,
            shippingDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)));

        var materialRepository = new RawMaterialRepository(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var orderRepository = new OrderRepository(dbContext);
        await materialRepository.AddAsync(material);
        await productRepository.AddAsync(product);
        await orderRepository.AddAsync(order);

        if (taskStatus is not null)
        {
            var task = new ProductionTask(
                Guid.NewGuid(),
                order.Id,
                order.Items.Single().Id,
                product.Id,
                1,
                status: taskStatus.Value,
                startedAt: taskStatus == ProductionTaskStatus.Completed ? DateTimeOffset.UtcNow : null,
                completedAt: taskStatus == ProductionTaskStatus.Completed ? DateTimeOffset.UtcNow : null);
            await new ProductionTaskRepository(dbContext).AddAsync(task);
            await new StockMovementRepository(dbContext).AddAsync(new StockMovement(
                Guid.NewGuid(),
                material.Id,
                StockMovementType.Outbound,
                2,
                "Production consumption",
                task.Id.ToString()));
        }

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

    private sealed class RecordingAuditService : IAuditService
    {
        public string? Action { get; private set; }

        public Task RecordAsync(
            string action,
            string entityName,
            string? entityId = null,
            object? before = null,
            object? after = null,
            CancellationToken cancellationToken = default)
        {
            Action = action;
            return Task.CompletedTask;
        }
    }

    private sealed class EmptyTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }

    private sealed class NoOpDomainEventPublisher : IDomainEventPublisher
    {
        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent => Task.CompletedTask;
    }

    private sealed class RecordingTransactionalUnitOfWork : ITransactionalUnitOfWork
    {
        public bool Executed { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            Executed = true;
            return await operation();
        }
    }
}
