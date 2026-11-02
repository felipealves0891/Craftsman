using System.Text.Json;
using Craftsman.App.Controllers;
using Craftsman.App.Services;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Production.Repositories;
using Craftsman.Domain.Repositories;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Domain.Sales.Repositories;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Craftsman.Infra.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Production;

public sealed class ProductionControllerTests
{
    [Fact]
    public async Task Production_calendar_uses_server_month_unless_a_date_is_selected()
    {
        await using var dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var clock = new FixedTimeProvider(new DateTimeOffset(2031, 6, 15, 8, 0, 0, TimeSpan.Zero));
        var controller = CreateController(dbContext, clock);

        var defaultView = Assert.IsType<ViewResult>(await controller.Index(status: null, plannedDate: null, CancellationToken.None));
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyCollection<Craftsman.App.Models.ProductionTaskListItemViewModel>>(defaultView.Model));
        Assert.Equal(new DateTime(2031, 6, 15), controller.ViewBag.ReferenceDate);

        await controller.Index(status: null, plannedDate: new DateOnly(2031, 5, 4), CancellationToken.None);
        Assert.Equal(new DateTime(2031, 5, 4), controller.ViewBag.ReferenceDate);

        var monthResult = Assert.IsType<JsonResult>(controller.CurrentMonth());
        var month = JsonSerializer.SerializeToElement(monthResult.Value);
        Assert.Equal(2031, month.GetProperty("year").GetInt32());
        Assert.Equal(6, month.GetProperty("month").GetInt32());

        var calendarResult = Assert.IsType<PartialViewResult>(await controller.Calendar(ProductionTaskStatus.Planned, CancellationToken.None));
        Assert.Equal("_Calendar", calendarResult.ViewName);
        Assert.Equal(ProductionTaskStatus.Planned, controller.ViewBag.Status);
        Assert.Null(controller.ViewBag.PlannedDate);
        Assert.Equal(new DateTime(2031, 6, 15), controller.ViewBag.ReferenceDate);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyCollection<Craftsman.App.Models.ProductionTaskListItemViewModel>>(calendarResult.Model));
    }

    [Fact]
    public async Task Advance_success_redirects_and_records_audit()
    {
        await using var dbContext = CreateDbContext();
        var order = CreateOrder(OrderStatus.ReadyForProduction, "PROD-CONTROLLER-START");
        var task = CreateTask(order);
        var orderRepository = new OrderRepository(dbContext);
        var taskRepository = new ProductionTaskRepository(dbContext);
        await orderRepository.AddAsync(order);
        await taskRepository.AddAsync(task);
        await dbContext.SaveChangesAsync();
        var auditService = new RecordingAuditService();
        var controller = CreateController(dbContext, new FixedTimeProvider(DateTimeOffset.UtcNow), auditService);
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new EmptyTempDataProvider());

        var result = Assert.IsType<RedirectToActionResult>(await controller.Advance(task.Id, "start", CancellationToken.None));

        Assert.Equal(nameof(ProductionController.Index), result.ActionName);
        Assert.Equal(AuditAction.ProductionAdvanced, auditService.Action);
        Assert.Equal("start", auditService.AfterAction);
        Assert.Empty(controller.TempData);
    }

    [Fact]
    public async Task Advance_invalid_transition_redirects_with_error_message()
    {
        await using var dbContext = CreateDbContext();
        var order = CreateOrder(OrderStatus.ReadyForProduction, "PROD-CONTROLLER-INVALID");
        var task = CreateTask(order);
        var taskRepository = new ProductionTaskRepository(dbContext);
        await taskRepository.AddAsync(task);
        await dbContext.SaveChangesAsync();
        var auditService = new RecordingAuditService();
        var controller = CreateController(dbContext, new FixedTimeProvider(DateTimeOffset.UtcNow), auditService);
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new EmptyTempDataProvider());

        var result = Assert.IsType<RedirectToActionResult>(await controller.Advance(task.Id, "complete", CancellationToken.None));

        Assert.Equal(nameof(ProductionController.Index), result.ActionName);
        Assert.Contains("expected InProduction", Assert.IsType<string>(controller.TempData["Error"]));
        Assert.Null(auditService.Action);
    }

    private static ProductionController CreateController(
        AppDbContext dbContext,
        TimeProvider timeProvider,
        IAuditService? auditService = null)
    {
        var scheduleService = new ProductionScheduleService(
            new ProductionTaskRepository(dbContext),
            new EmptyProductRepository(),
            new OrderRepository(dbContext),
            new UnitOfWork(dbContext),
            timeProvider);

        return new ProductionController(scheduleService, auditService ?? new RecordingAuditService(), timeProvider);
    }

    private static AppDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Order CreateOrder(OrderStatus status, string externalOrderId)
        => new(
            Guid.NewGuid(),
            new OrderOrigin("Manual", externalOrderId),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Bolsa", 1, new Money(30, "BRL"))],
            status);

    private static ProductionTask CreateTask(Order order)
        => new(Guid.NewGuid(), order.Id, order.Items.Single().Id, Guid.NewGuid(), 1);

    private sealed class FixedTimeProvider(DateTimeOffset localNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => localNow.ToUniversalTime();

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class EmptyProductRepository : IProductRepository
    {
        public Task<Craftsman.Domain.ProductCatalog.Entities.Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Craftsman.Domain.ProductCatalog.Entities.Product?>(null);

        public Task<IReadOnlyCollection<Craftsman.Domain.ProductCatalog.Entities.Product>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<Craftsman.Domain.ProductCatalog.Entities.Product>>([]);

        public Task AddAsync(Craftsman.Domain.ProductCatalog.Entities.Product product, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(Craftsman.Domain.ProductCatalog.Entities.Product product, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class RecordingAuditService : IAuditService
    {
        public string? Action { get; private set; }

        public object? AfterAction { get; private set; }

        public Task RecordAsync(
            string action,
            string entityName,
            string? entityId = null,
            object? before = null,
            object? after = null,
            CancellationToken cancellationToken = default)
        {
            Action = action;
            AfterAction = after?.GetType().GetProperty("Action")?.GetValue(after);
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
}
