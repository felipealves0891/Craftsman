using Craftsman.Domain.Events;
using Craftsman.Domain.Inventory.Entities;
using Craftsman.Domain.Inventory.Services;
using Craftsman.Domain.ProductCatalog.Entities;
using Craftsman.Domain.ProductCatalog.Repositories;
using Craftsman.Domain.Production.Entities;
using Craftsman.Domain.Production.Services;
using Craftsman.Domain.Sales.Entities;
using Craftsman.Domain.Sales.ObjectValues;
using Craftsman.Infra.Persistence;
using Craftsman.Infra.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Craftsman.Tests.Production;

public sealed class OrderProductionPlanningReversalTests
{
    [Fact]
    public async Task Revert_compensates_stock_deletes_tasks_and_makes_order_plannable_again()
    {
        await using var dbContext = CreateDbContext();
        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "m");
        var product = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(material.Id, 2)]);
        var order = CreateOrder(product.Id, OrderStatus.ReadyForProduction);
        var task = new ProductionTask(Guid.NewGuid(), order.Id, order.Items.Single().Id, product.Id, 1);
        var outbound = new StockMovement(
            Guid.NewGuid(),
            material.Id,
            StockMovementType.Outbound,
            2,
            "Production consumption",
            task.Id.ToString(),
            unitCostAmount: 7.5m);

        var materialRepository = new RawMaterialRepository(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var orderRepository = new OrderRepository(dbContext);
        var taskRepository = new ProductionTaskRepository(dbContext);
        var movementRepository = new StockMovementRepository(dbContext);
        await materialRepository.AddAsync(material);
        await productRepository.AddAsync(product);
        await orderRepository.AddAsync(order);
        await taskRepository.AddAsync(task);
        await movementRepository.AddAsync(outbound);
        await dbContext.SaveChangesAsync();

        var service = CreateService(
            orderRepository,
            productRepository,
            taskRepository,
            movementRepository,
            new UnitOfWork(dbContext),
            dbContext);

        var reverted = await service.RevertAsync(order.Id);

        Assert.True(reverted);
        Assert.Equal(OrderStatus.Normalized, (await orderRepository.GetByIdAsync(order.Id))?.Status);
        Assert.Empty(await taskRepository.ListByOrderAsync(order.Id));
        var movements = await movementRepository.ListByBusinessReferenceAsync(task.Id.ToString());
        Assert.Single(movements);
        var compensation = Assert.Single(await movementRepository.ListByBusinessReferenceAsync($"production-reversal:{outbound.Id}"));
        Assert.Equal(StockMovementType.Inbound, compensation.Type);
        Assert.Equal(outbound.Quantity, compensation.Quantity);
        Assert.Equal(outbound.UnitCostAmount, compensation.UnitCostAmount);
        Assert.Equal(0, await movementRepository.GetBalanceAsync(material.Id));

        var repeated = await service.RevertAsync(order.Id);

        Assert.False(repeated);
        Assert.Single(await movementRepository.ListByBusinessReferenceAsync($"production-reversal:{outbound.Id}"));
    }

    [Fact]
    public async Task Revert_rejects_completed_task_without_partial_changes()
    {
        await using var dbContext = CreateDbContext();
        var material = new RawMaterial(Guid.NewGuid(), "Tecido", "m");
        var product = new Product(Guid.NewGuid(), "Bolsa", billOfMaterials: [new BillOfMaterialsItem(material.Id, 2)]);
        var order = CreateOrder(product.Id, OrderStatus.InProduction);
        var task = new ProductionTask(
            Guid.NewGuid(),
            order.Id,
            order.Items.Single().Id,
            product.Id,
            1,
            status: ProductionTaskStatus.Completed,
            startedAt: DateTimeOffset.UtcNow,
            completedAt: DateTimeOffset.UtcNow);

        var materialRepository = new RawMaterialRepository(dbContext);
        var productRepository = new ProductRepository(dbContext);
        var orderRepository = new OrderRepository(dbContext);
        var taskRepository = new ProductionTaskRepository(dbContext);
        var movementRepository = new StockMovementRepository(dbContext);
        await materialRepository.AddAsync(material);
        await productRepository.AddAsync(product);
        await orderRepository.AddAsync(order);
        await taskRepository.AddAsync(task);
        await movementRepository.AddAsync(new StockMovement(
            Guid.NewGuid(),
            material.Id,
            StockMovementType.Outbound,
            2,
            "Production consumption",
            task.Id.ToString()));
        await dbContext.SaveChangesAsync();

        var service = CreateService(
            orderRepository,
            productRepository,
            taskRepository,
            movementRepository,
            new UnitOfWork(dbContext),
            dbContext);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RevertAsync(order.Id));

        Assert.Contains("completed", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(OrderStatus.InProduction, (await orderRepository.GetByIdAsync(order.Id))?.Status);
        Assert.NotNull(await taskRepository.GetByIdAsync(task.Id));
        Assert.DoesNotContain(
            await movementRepository.ListAsync(),
            movement => movement.BusinessReference?.StartsWith("production-reversal:", StringComparison.Ordinal) == true);
    }

    private static OrderProductionPlanningService CreateService(
        OrderRepository orderRepository,
        ProductRepository productRepository,
        ProductionTaskRepository taskRepository,
        StockMovementRepository movementRepository,
        UnitOfWork unitOfWork,
        AppDbContext dbContext)
    {
        return new OrderProductionPlanningService(
            orderRepository,
            new ProductionPlanner(
                productRepository,
                new InventoryService(new RawMaterialRepository(dbContext), movementRepository)),
            taskRepository,
            unitOfWork,
            new NoOpDomainEventPublisher(),
            NullLogger<OrderProductionPlanningService>.Instance,
            movementRepository);
    }

    private static Order CreateOrder(Guid productId, OrderStatus status)
    {
        return new Order(
            Guid.NewGuid(),
            new OrderOrigin("Manual", Guid.NewGuid().ToString("N")),
            [new OrderItem(Guid.NewGuid(), "ITEM-1", "Bolsa", 1, new Money(30, "BRL"), productId)],
            status,
            shippingDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)));
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
            where TEvent : IDomainEvent
            => Task.CompletedTask;
    }
}
