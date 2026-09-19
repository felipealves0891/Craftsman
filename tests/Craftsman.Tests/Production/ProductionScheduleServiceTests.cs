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

    private static ProductionScheduleService CreateService(AppDbContext dbContext)
    {
        return new ProductionScheduleService(
            new ProductionTaskRepository(dbContext),
            new EmptyProductRepository(),
            new OrderRepository(dbContext),
            new UnitOfWork(dbContext));
    }

    private static Order CreateOrder(OrderStatus status, string externalOrderId)
    {
        return new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", externalOrderId),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Bolsa", 1, new Money(30, "BRL"))],
            status);
    }

    private static ProductionTask CreateTask(Guid orderId)
    {
        return new ProductionTask(Guid.NewGuid(), orderId, Guid.NewGuid(), Guid.NewGuid(), 1);
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
