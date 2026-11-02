using Craftsman.App.Services;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Craftsman.Tests.Production;

public sealed class ProductionScheduleServiceTests
{
    [Fact]
    public async Task List_without_planned_date_uses_current_month_even_when_another_month_has_tasks()
    {
        await using var dbContext = CreateDbContext();
        var taskRepository = new ProductionTaskRepository(dbContext);
        var currentMonthTask = CreateTask(
            Guid.NewGuid(),
            new DateTimeOffset(2031, 6, 12, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2031, 6, 12, 11, 0, 0, TimeSpan.Zero));
        var previousMonthTask = CreateTask(
            Guid.NewGuid(),
            new DateTimeOffset(2031, 5, 12, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2031, 5, 12, 11, 0, 0, TimeSpan.Zero));

        await taskRepository.AddAsync(currentMonthTask);
        await taskRepository.AddAsync(previousMonthTask);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, new FixedTimeProvider(new DateTimeOffset(2031, 6, 15, 8, 0, 0, TimeSpan.Zero)));

        var tasks = await service.ListAsync();

        Assert.Equal(new[] { currentMonthTask.Id }, tasks.Select(task => task.Id));
    }

    [Fact]
    public async Task List_with_planned_date_preserves_selected_month()
    {
        await using var dbContext = CreateDbContext();
        var taskRepository = new ProductionTaskRepository(dbContext);
        var currentMonthTask = CreateTask(
            Guid.NewGuid(),
            new DateTimeOffset(2031, 6, 12, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2031, 6, 12, 11, 0, 0, TimeSpan.Zero));
        var selectedMonthTask = CreateTask(
            Guid.NewGuid(),
            new DateTimeOffset(2031, 5, 12, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2031, 5, 12, 11, 0, 0, TimeSpan.Zero));

        await taskRepository.AddAsync(currentMonthTask);
        await taskRepository.AddAsync(selectedMonthTask);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, new FixedTimeProvider(new DateTimeOffset(2031, 6, 15, 8, 0, 0, TimeSpan.Zero)));

        var tasks = await service.ListAsync(plannedDate: new DateOnly(2031, 5, 1));

        Assert.Equal(new[] { selectedMonthTask.Id }, tasks.Select(task => task.Id));
    }

    [Fact]
    public async Task List_uses_fallback_references_when_task_order_is_missing()
    {
        await using var dbContext = CreateDbContext();
        var taskRepository = new ProductionTaskRepository(dbContext);
        var task = CreateTask(
            Guid.NewGuid(),
            new DateTimeOffset(2031, 6, 12, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2031, 6, 12, 11, 0, 0, TimeSpan.Zero));
        await taskRepository.AddAsync(task);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, new FixedTimeProvider(new DateTimeOffset(2031, 6, 15, 8, 0, 0, TimeSpan.Zero)));

        var item = Assert.Single(await service.ListAsync());

        Assert.Equal(task.OrderId.ToString()[..8], item.OrderReference);
        Assert.Equal("Item sem descricao", item.ItemDescription);
        Assert.Equal(task.OrderId.ToString()[..8], item.ExternalOrderId);
    }

    [Fact]
    public async Task List_uses_item_fallback_when_order_does_not_contain_task_item()
    {
        await using var dbContext = CreateDbContext();
        var orderRepository = new OrderRepository(dbContext);
        var taskRepository = new ProductionTaskRepository(dbContext);
        var order = CreateOrder(OrderStatus.ReadyForProduction, "PROD-NO-ITEM");
        var task = CreateTask(
            order.Id,
            new DateTimeOffset(2031, 6, 12, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2031, 6, 12, 11, 0, 0, TimeSpan.Zero));
        await orderRepository.AddAsync(order);
        await taskRepository.AddAsync(task);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, new FixedTimeProvider(new DateTimeOffset(2031, 6, 15, 8, 0, 0, TimeSpan.Zero)));

        var item = Assert.Single(await service.ListAsync());

        Assert.Equal("Manual PROD-NO-ITEM", item.OrderReference);
        Assert.Equal("Item sem descricao", item.ItemDescription);
        Assert.Equal("PROD-NO-ITEM", item.ExternalOrderId);
    }

    [Fact]
    public async Task Completing_in_production_task_sets_completion_time()
    {
        await using var dbContext = CreateDbContext();
        var order = CreateOrder(OrderStatus.InProduction, "PROD-COMPLETE");
        var task = CreateTask(order.Id, status: ProductionTaskStatus.InProduction);
        var orderRepository = new OrderRepository(dbContext);
        var taskRepository = new ProductionTaskRepository(dbContext);
        await orderRepository.AddAsync(order);
        await taskRepository.AddAsync(task);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        await service.AdvanceAsync(task.Id, "complete");

        var updated = await taskRepository.GetByIdAsync(task.Id);
        Assert.Equal(ProductionTaskStatus.Completed, updated?.Status);
        Assert.NotNull(updated?.CompletedAt);
    }

    [Fact]
    public async Task Cancelling_planned_task_persists_cancelled_status()
    {
        await using var dbContext = CreateDbContext();
        var taskRepository = new ProductionTaskRepository(dbContext);
        var task = CreateTask(Guid.NewGuid());
        await taskRepository.AddAsync(task);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        await service.AdvanceAsync(task.Id, "cancel");

        var updated = await taskRepository.GetByIdAsync(task.Id);
        Assert.Equal(ProductionTaskStatus.Cancelled, updated?.Status);
    }

    [Fact]
    public async Task Advancing_missing_task_reports_not_found()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AdvanceAsync(Guid.NewGuid(), "start"));

        Assert.Contains("Production task was not found", exception.Message);
    }

    [Fact]
    public async Task Advancing_with_unknown_action_reports_unsupported_action()
    {
        await using var dbContext = CreateDbContext();
        var taskRepository = new ProductionTaskRepository(dbContext);
        var task = CreateTask(Guid.NewGuid());
        await taskRepository.AddAsync(task);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AdvanceAsync(task.Id, "pause"));

        Assert.Contains("Unsupported production action 'pause'", exception.Message);
    }

    [Fact]
    public async Task Starting_first_task_marks_order_as_in_production()
    {
        await using var dbContext = CreateDbContext();
        var orderRepository = new OrderRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var order = CreateOrder(OrderStatus.ReadyForProduction, "PROD-001");
        var task = CreateTask(order.Id);

        await orderRepository.AddAsync(order);
        await productionTaskRepository.AddAsync(task);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        await service.AdvanceAsync(task.Id, "start");

        var loaded = await orderRepository.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.InProduction, loaded?.Status);
    }

    [Fact]
    public async Task Starting_another_task_keeps_order_in_production()
    {
        await using var dbContext = CreateDbContext();
        var orderRepository = new OrderRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var order = CreateOrder(OrderStatus.InProduction, "PROD-002");
        var task = CreateTask(order.Id);

        await orderRepository.AddAsync(order);
        await productionTaskRepository.AddAsync(task);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        await service.AdvanceAsync(task.Id, "start");

        var loaded = await orderRepository.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.InProduction, loaded?.Status);
    }

    [Fact]
    public async Task Starting_task_rejects_order_that_is_not_ready_for_production()
    {
        await using var dbContext = CreateDbContext();
        var orderRepository = new OrderRepository(dbContext);
        var productionTaskRepository = new ProductionTaskRepository(dbContext);
        var order = CreateOrder(OrderStatus.Normalized, "PROD-BLOCKED");
        var task = CreateTask(order.Id);

        await orderRepository.AddAsync(order);
        await productionTaskRepository.AddAsync(task);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AdvanceAsync(task.Id, "start"));

        Assert.Contains("cannot start production", exception.Message);
    }

    private static ProductionScheduleService CreateService(AppDbContext dbContext, TimeProvider? timeProvider = null)
    {
        return new ProductionScheduleService(
            new ProductionTaskRepository(dbContext),
            new EmptyProductRepository(),
            new OrderRepository(dbContext),
            new UnitOfWork(dbContext),
            timeProvider ?? new FixedTimeProvider(DateTimeOffset.UtcNow));
    }

    private static Order CreateOrder(OrderStatus status, string externalOrderId)
    {
        return new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", externalOrderId),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Bolsa", 1, new Money(30, "BRL"))],
            status);
    }

    private static ProductionTask CreateTask(
        Guid orderId,
        DateTimeOffset? plannedStartAt = null,
        DateTimeOffset? plannedAt = null,
        ProductionTaskStatus status = ProductionTaskStatus.Planned)
    {
        return new ProductionTask(
            Guid.NewGuid(), orderId, Guid.NewGuid(), Guid.NewGuid(), 1,
            status: status,
            plannedStartAt: plannedStartAt,
            plannedAt: plannedAt);
    }

    private sealed class FixedTimeProvider(DateTimeOffset localNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => localNow.ToUniversalTime();

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
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
}
